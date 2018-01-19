using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lua
{
    public class lparser
    {
        public struct table
        {
            public string md5;
            public string tablename;
            public List<config> configs;
        }

        public struct config
        {
            public string key;
            public List<property> properties;
        }

        public struct property
        {
            public string propertyname;
            public string value;
        }

        public static void LuaY_Parser(char[] buff, int offset)
        {
            StreamReader sr = new StreamReader(@"");
            char[] cs = sr.ReadToEnd().ToCharArray();
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
            p.propertyname = llex_lite.buff2str();/* read key */
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
                tablename = llex_lite.buff2str(),
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
            llex_lite.llex(sr);
            return llex_lite.buff2str();
        }

        public static void read_file(string path)
        {
            StreamReader sr = new StreamReader(path);
            table t = read_table(sr);
            StringBuilder sb = new StringBuilder();
            Console.WriteLine("md5 = " + t.md5 + " tablename = " + t.tablename);
            for(int i = 0; i < t.configs.Count; i++)
            {
                config conf = t.configs[i];
                sb.Append(conf.key + " = ");
                for(int j = 0; j < conf.properties.Count; j++)
                {
                    property p = conf.properties[j];
                    sb.Append(p.propertyname + " = " + p.value + " ");
                }
                Console.WriteLine(sb.ToString());
                sb.Clear();
            }
        }
    }
}
