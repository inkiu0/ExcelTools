using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lua.llex_lite;

namespace Lua
{
    public class lparser
    {
        static string configformat = "[{0}] = {{";
        static string kvformat = "{0} = {1}";
        public struct table
        {
            public string md5;
            public string name;
            public List<config> configs;

            public string GenString(Func<string> callback)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("--" + md5 + "\n");
                sb.Append(name + " = {\n");
                for (int i = 0; i < configs.Count; i++)
                {
                    sb.Append("\t");
                    sb.Append(configs[i].GenString());
                    if (i < configs.Count - 1)
                        sb.Append(",");
                    sb.Append("\n");
                }
                sb.Append("}\n");
                sb.Append(callback());
                sb.Append("return " + name);
                return sb.ToString();
            }
        }

        public struct config
        {
            public string key;
            public List<property> properties;

            public string GenString()
            {
                StringBuilder sb = new StringBuilder(string.Format(configformat, key));
                for(int i = 0; i < properties.Count; i++)
                {
                    sb.Append(properties[i].GenString());
                    if (i < properties.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                return sb.ToString();
            }
        }

        public struct property
        {
            public string name;
            public string value;

            public string GenString()
            {
                return string.Format(kvformat, name, value);
            }
        }

        static string read_name(StreamReader sr)
        {
            string ret = null;
            StringBuilder sb = new StringBuilder();
            while (!sr.EndOfStream && sr.Peek() != '=')
            {
                sb.Append((char)sr.Read());
            }
            return ret;
        }

        static property read_property(StreamReader sr)
        {
            property p = new property();
            p.name = llex_lite.buff2str();/* read key */
            llex_lite.llex(sr); p.value = llex_lite.buff2str();/* read val */
            return p;
        }

        static config read_config(StreamReader sr)
        {
            //llex_lite.llex(sr); /* read key */
            config config = new config
            {
                key = llex_lite.buff2str(),/* read key */
                properties = new List<property>()
            };
            llex_lite.llex(sr, true);/* skip '{' */
            string k, v = null;
            while (!sr.EndOfStream && llex_lite.llex(sr) != '}')
            {
                config.properties.Add(read_property(sr));
            }
            return config;
        }

        static table read_table(StreamReader sr)
        {
            string md5Str = read_md5comment(sr);
            llex_lite.llex(sr); /* read key */
            table t = new table
            {
                md5 = md5Str,
                name = llex_lite.buff2str(),
                configs = new List<config>()
            };
            llex_lite.llex(sr, true);/* skip '{' */
            while(!sr.EndOfStream && llex_lite.llex(sr) != '}')
            {
                t.configs.Add(read_config(sr));
            }
            return t;
        }

        static string read_md5comment(StreamReader sr)
        {
            int e = llex_lite.llex(sr);
            Debug.Assert(e == (int)LEXTYPE.COMMENT);
            return llex_lite.buff2str();
        }

        static void read_file(string path)
        {
            StreamReader sr = new StreamReader(path);
            table t = read_table(sr);
            StringBuilder sb = new StringBuilder();
            Console.WriteLine("md5 = " + t.md5 + " tablename = " + t.name);
            for(int i = 0; i < t.configs.Count; i++)
            {
                config conf = t.configs[i];
                sb.Append(conf.key + " = ");
                for(int j = 0; j < conf.properties.Count; j++)
                {
                    property p = conf.properties[j];
                    sb.Append(p.name + " = " + p.value + " ");
                }
                Console.WriteLine(sb.ToString());
                sb.Clear();
            }
        }

        public static table parse(string path)
        {
            StreamReader sr = new StreamReader(path);
            return read_table(sr);
        }
    }
}
