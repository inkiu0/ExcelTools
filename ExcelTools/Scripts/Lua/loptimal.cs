using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lua.lparser;

namespace Lua
{
    class loptimal
    {
        enum Optimization
        {
            Normal,
            Group,
            Skill
        }

        static Setting _NormalSetting = new Setting(Optimization.Normal, null);
        struct Setting
        {
            public Optimization type;
            public List<string> EigenValues;

            public Setting(Optimization t, List<string> vs)
            {
                type = t;
                EigenValues = vs;
            }
        }

        #region 非Normal类型的LuaTable记录如下，并记录分组需要的特征值。
        static Dictionary<string, Setting> _OptimalSetting = new Dictionary<string, Setting>()
        {
            {"Table_Equip",
                new Setting(Optimization.Group, new List<string>()
                { "Icon", "CanEquip", "Type", "EquipType" })
            },
            {"Table_Item",
                new Setting(Optimization.Group, new List<string>()
                { "Icon", "Type" })
            },
            {"Table_Monster",
                new Setting(Optimization.Group, new List<string>()
                { "Type", "Race", "Nature" })
            },
            {"Table_Npc",
                new Setting(Optimization.Group, new List<string>()
                { "Type", "Race", "Nature" })
            },
            {"Table_Dialog",
                new Setting(Optimization.Group, new List<string>()
                { "Text" })
            },
            {"Table_Skill", new Setting(Optimization.Skill, null) }
        };
        #endregion

        public static void optimal(string path, string outpath)
        {
            table table = lparser.parse(path);
            Setting ts = GetTableSetting(table);
            string ret = string.Empty;
            switch(ts.type)
            {
                case Optimization.Normal:
                    ret = optimal_normal(table);
                    break;
                case Optimization.Group:
                    ret = optimal_group(table);
                    break;
                case Optimization.Skill:
                    break;
            }

            using (StreamWriter sw = File.CreateText(outpath))
            {
                sw.Write(ret);
            }
        }

        #region Normal优化，即只抽取公共字段，将重复次数最多的值作为默认值
        static Setting GetTableSetting(table table)
        {
            Setting type = _NormalSetting;
            if (_OptimalSetting.ContainsKey(table.name))
                type = _OptimalSetting[table.name];
            return type;
        }

        /// <summary>
        /// 以LuaTable的第一条配置的所有属性作为base
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        static Dictionary<string, Dictionary<string, int>> GetBaseProperties(table table)
        {
            Dictionary<string, Dictionary<string, int>> basedic = new Dictionary<string, Dictionary<string, int>>();
            if (table.configs.Count > 0)
            {
                config baseconfig = table.configs[0];
                for (int i = 0; i < baseconfig.properties.Count; i++)
                {
                    basedic.Add(baseconfig.properties[i].name, new Dictionary<string, int>{ { baseconfig.properties[i].value, 1 } });
                }
            }
            return basedic;
        }

        static Dictionary<string, string> CountBaseKVs(Dictionary<string, Dictionary<string, int>> basedic)
        {
            Dictionary<string, string> basekv = new Dictionary<string, string>();
            string val = string.Empty;
            int count = 1;
            foreach(var item in basedic)
            {
                foreach(var vs in item.Value)
                {
                    if(vs.Value > count)
                    {
                        val = vs.Key;
                        count = vs.Value;
                    }
                }
                basekv.Add(item.Key, val);
                val = string.Empty; count = 1;
            }
            return basekv;
        }

        static void SwapPropertiesBase(Dictionary<string, Dictionary<string, int>> o, Dictionary<string, Dictionary<string, int>> n)
        {
            Dictionary<string, Dictionary<string, int>> temp = o;
            o = n;
            temp.Clear();
            n = temp;
        }

        //static string gen_metatable(Dictionary<string, Dictionary<string, int>> basedic)

        static string gen_normal(table table, Dictionary<string, string> basekv)
        {
            //去除不需要生成的属性
            property temp;
            for (int i = 0; i < table.configs.Count; i++)
            {
                for (int j = 0; j < table.configs[i].properties.Count; j++)
                {
                    temp = table.configs[i].properties[j];
                    if (basekv.ContainsKey(temp.name) && basekv[temp.name] == temp.value)
                        table.configs.RemoveAt(j);
                }
            }

            Func<string> genmeta = () =>
            {
                string meta = "local __default = {\n";
                foreach(var item in basekv)
                    meta = string.Format("{0}\t{1} = {2},\n", meta, item.Key, item.Value);
                meta = string.Format("{0}}}\ndo\n\tlocal base = {{\n\t\t__index = __default,\n\t\t--__newindex = function()\n\t\t\t--禁止写入新值\n\t\t\t--error(\"Attempt to modify read-only table\")\n\t\t--end\n\t}}\n\tfor k, v in pairs({1}) do\n\t\tsetmetatable(v, base)\n\tend\n\tbase.__metatable = false\nend\n", meta, table.name);
                return meta;
            };
            return table.GenString(genmeta);
        }

        static string optimal_normal(table table)
        {
            Dictionary<string, Dictionary<string, int>> basedic = GetBaseProperties(table);
            Dictionary<string, Dictionary<string, int>> newbase = new Dictionary<string, Dictionary<string, int>>();
            property temp;
            //configs在构造时即为 new List<string>()，一定不为空
            for(int i = 0; i < table.configs.Count; i++)
            {
                for(int j = 0; j < table.configs[i].properties.Count; j++)
                {
                    temp = table.configs[i].properties[j];
                    if (basedic.ContainsKey(temp.name))
                    {
                        newbase.Add(temp.name, basedic[temp.name]);
                        if (newbase[temp.name].ContainsKey(temp.value))
                            newbase[temp.name][temp.value]++;
                        else
                            newbase[temp.name].Add(temp.value, 1);
                    }
                }
                SwapPropertiesBase(basedic, newbase);
            }
            return gen_normal(table, CountBaseKVs(basedic));
        }
        #endregion

        #region Group优化，即分组优化，自动将特征值一致的配置分为同一组。然后抽取相同的值，作为元表。

        static void partition_grop(table table, List<string> eigenvalues)
        {
            Dictionary<string, List<string>> group = new Dictionary<string, List<string>>();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            for (int i = 0; i < eigenvalues.Count; i++)
                dic.Add(eigenvalues[i], i);
            int total = dic.Count;
            string[] strs = new string[total];
            for(int i = 0; i < table.configs.Count; i++)
            {
                for(int j = 0; j < table.configs[i].properties.Count; j++)
                {
                    if(dic.ContainsKey(table.configs[i].properties[i].name))
                    {
                        strs[dic[table.configs[i].properties[i].name]] = table.configs[i].properties[i].value;
                        total--;
                    }
                }
                if (total == 0)
                {
                    string key = string.Join(string.Empty, strs);
                    if (group.ContainsKey(key))
                        group[key].Add(table.configs[i].key);
                    else
                        group.Add(key, new List<string> { table.configs[i].key });
                }
                total = dic.Count;
            }
        }

        static string optimal_group(table table)
        {
            return string.Empty;
        }
        #endregion
    }
}
