using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace GTA_JSON_TO_INI {

    class Program {

        static void Main( string[] args ) {

            if( !generate( "gta3.json", "SCM.INI" ) )
                Console.WriteLine( "Произошла херня :(" );
            
            if( !generate( "vc.json", "VCSCM.INI" ) )
                Console.WriteLine( "Произошла херня :(" );

            if( !generate( "sa.json", "SASCM.INI" ) )
                Console.WriteLine( "Произошла херня :(" );

            Console.ReadKey();
        }

        static string normalizeArgName( string inputName ) {
            return Regex.Replace( inputName, "([A-Z])", "_$1", RegexOptions.Compiled ).ToLower().Trim( '_', ' ' );
        }

        static bool generate( string pathToJson, string pathToIni ) {
            var nowTime = DateTime.Now;
            Console.WriteLine( $"Читаю файл '{pathToJson}'..." );
            var values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>( File.ReadAllText( pathToJson ) );

            StringBuilder sb = new StringBuilder(), args = new StringBuilder();
            Console.WriteLine( $"Пытаюсь собрать '{pathToIni}'..." );

            sb.AppendLine( $@"; GTA Modding Community Opcode Database
;
; For more info, visit: 
; https://www.gtagmodding.com/opcode-database/
; https://gtamods.com/wiki/List_of_opcodes
; https://docs.sannybuilder.com/edit-modes/opcodes-list-scm.ini
;
; d% = anything
; p% = label pointer
; o% = models all types
; m% = .ide models only
; g% = gxt reference
; x% = external script
; k% = 128-byte null-terminated string

DATE={nowTime.Year}-{nowTime.Month}-{nowTime.Day}

[OPCODES]" );


            foreach( JObject currentExtension in values[ "extensions" ] ) {

                if( currentExtension[ "name" ].ToString() != "default" )
                    continue;

                foreach( JObject currentCommand in currentExtension[ "commands" ] ) {

                    string opcode = currentCommand[ "id" ].ToString();
                    int numParams = int.Parse( currentCommand[ "num_params" ].ToString() );
                    string name = currentCommand[ "name" ].ToString().ToLower();

                    bool is_condition = false,
                         is_unsupported = false,
                         is_nop = false,
                         is_variadic = false,
                         //is_branch = false,
                         //is_keyword = false,
                         //is_segment = false,
                         //is_constructor = false,
                         //is_overload = false,
                         //is_static = false,
                         //is_destructor = false,
                         hasAttrs = false;

                    if( currentCommand.ContainsKey( "attrs" ) ) {
                        var attrObj = currentCommand[ "attrs" ] as JObject;
                        is_condition = attrObj.ContainsKey( "is_condition" );
                        is_unsupported = attrObj.ContainsKey( "is_unsupported" );
                        is_nop = attrObj.ContainsKey( "is_nop" );
                        is_variadic = attrObj.ContainsKey( "is_variadic" );
                        //is_branch = attrObj.ContainsKey( "is_branch" );
                        //is_keyword = attrObj.ContainsKey( "is_keyword" );
                        //is_segment = attrObj.ContainsKey( "is_segment" );
                        //is_constructor = attrObj.ContainsKey( "is_constructor" );
                        //is_overload = attrObj.ContainsKey( "is_overload" );
                        //is_static = attrObj.ContainsKey( "is_static" );
                        //is_destructor = attrObj.ContainsKey( "is_destructor" );
                        hasAttrs = true;
                    }

                    string offset = "", comments = "", hide_unsupported = "";
                    if( hasAttrs ) {
                        if( is_unsupported ) {
                            hide_unsupported = "; ";
                            comments += " ; UNSUPPORTED";
                        }
                        if( is_condition )
                            offset = "  ";
                        if( is_variadic )
                            numParams = -1;
                        if( is_nop )
                            comments += " ; NOP";
                    }
                    args.Clear();
                    var arrgIndexCounter = 1;
                    //if( opcode == "05B6" ) {
                    //    string s = "";
                    //    s += 1;
                    //}
                    if( currentCommand.ContainsKey( "input" ) ) {
                        var inputArrg = currentCommand[ "input" ] as JArray;
                        foreach( JObject arg in inputArrg ) {
                            var arg_name = arg[ "name" ].ToString();
                            var arg_type = arg[ "type" ].ToString();
                            var insertedNumber = "d";
                            if( arg_name == "self" )
                                arg_name = $"{arg_type}_self";
                            switch( arg_type ) {
                                case "label":
                                insertedNumber = "p";
                                break;
                                case "float":
                                insertedNumber = "f";
                                break;
                                case "bool":
                                insertedNumber = "b";
                                break;
                                case "gxt_key":
                                insertedNumber = "g";
                                break;
                                case "zone_key":
                                insertedNumber = "z";
                                break;
                                case "script_id":
                                insertedNumber = "x";
                                break;
                                case "string128":
                                insertedNumber = "k";
                                break;
                                //case "":
                                //insertedNumber = "o";
                                //break;
                                //case "":
                                //insertedNumber = "m";
                                //break;
                            }
                            args.Append( $" {normalizeArgName( arg_name )} %{arrgIndexCounter}{insertedNumber}%" );
                            arrgIndexCounter += 1;
                        }
                    }
                    if( currentCommand.ContainsKey( "output" ) ) {
                        var outputArrg = currentCommand[ "output" ] as JArray;
                        args.Append( " output" );
                        foreach( JObject arg in outputArrg ) {
                            args.Append( $" {normalizeArgName( arg[ "name" ].ToString() )} %{arrgIndexCounter}d%" );
                            arrgIndexCounter += 1;
                        }
                    }
                    sb.AppendLine( $"{hide_unsupported}{opcode}={numParams},{offset}{name}{Regex.Replace( args.ToString(), " {1,}", " " )}{comments}" );
                }

            }
            File.WriteAllText( pathToIni, sb.ToString() );
            //string s = "";
            //s += 1;
            Console.WriteLine( $"Создано '{pathToIni}'!" );
            return true;
        }

    }

}