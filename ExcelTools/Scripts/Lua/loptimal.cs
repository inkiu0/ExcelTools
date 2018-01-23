﻿using System;
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
            Setting ts = get_table_setting(table);
            string ret = string.Empty;
            switch(ts.type)
            {
                case Optimization.Normal:
                    ret = optimal_normal(table);
                    break;
                case Optimization.Group:
                    ret = optimal_group(table, ts);
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
        static Setting get_table_setting(table table)
        {
            Setting type = _NormalSetting;
            if (_OptimalSetting.ContainsKey(table.name))
                type = _OptimalSetting[table.name];
            return type;
        }

        /// <summary>
        /// 以LuaTable的第一条配置的所有属性作为base
        /// </summary>
        /// <param name="configs"></param>
        /// <returns></returns>
        static Dictionary<string, Dictionary<string, int>> get_base_properties(List<config> configs)
        {
            Dictionary<string, Dictionary<string, int>> basedic = new Dictionary<string, Dictionary<string, int>>();
            if (configs.Count > 0)
            {
                config baseconfig = configs[0];
                for (int i = 0; i < baseconfig.properties.Count; i++)
                {
                    basedic.Add(baseconfig.properties[i].name, new Dictionary<string, int>{ { baseconfig.properties[i].value, 1 } });
                }
            }
            return basedic;
        }

        static Dictionary<string, string> count_baseKVs(Dictionary<string, Dictionary<string, int>> basedic)
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

        static void swap_properties_base(Dictionary<string, Dictionary<string, int>> o, Dictionary<string, Dictionary<string, int>> n)
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

        static Dictionary<string, string> get_basekvs(List<config> configs)
        {
            Dictionary<string, Dictionary<string, int>> basedic = get_base_properties(configs);
            Dictionary<string, Dictionary<string, int>> newbase = new Dictionary<string, Dictionary<string, int>>();
            property temp;
            //configs在构造时即为 new List<string>()，一定不为空
            for (int i = 0; i < configs.Count; i++)
            {
                for (int j = 0; j < configs[i].properties.Count; j++)
                {
                    temp = configs[i].properties[j];
                    if (basedic.ContainsKey(temp.name))
                    {
                        newbase.Add(temp.name, basedic[temp.name]);
                        if (newbase[temp.name].ContainsKey(temp.value))
                            newbase[temp.name][temp.value]++;
                        else
                            newbase[temp.name].Add(temp.value, 1);
                    }
                }
                swap_properties_base(basedic, newbase);
            }
            return count_baseKVs(basedic);
        }

        static string optimal_normal(table table)
        {
            return gen_normal(table, get_basekvs(table.configs));
        }
        #endregion

        #region Group优化，即分组优化，自动将特征值一致的配置分为同一组。然后抽取相同的值，作为元表。

        static Dictionary<string, List<config>> partition_grop(table table, List<string> eigenvalues)
        {
            Dictionary<string, List<config>> group = new Dictionary<string, List<config>>();
            int total = eigenvalues.Count;
            string[] strs = new string[total];
            int idx;
            for(int i = 0; i < table.configs.Count; i++)
            {
                for(int j = 0; j < table.configs[i].properties.Count; j++)
                {
                    idx = eigenvalues.IndexOf(table.configs[i].properties[i].name);
                    if(idx > -1)
                    {
                        strs[idx] = table.configs[i].properties[i].value;
                        total--;
                    }
                }
                if (total == 0)
                {
                    string key = string.Join(string.Empty, strs);
                    if (group.ContainsKey(key))
                        group[key].Add(table.configs[i]);
                    else
                        group.Add(key, new List<config> { table.configs[i] });
                }
                total = eigenvalues.Count;
            }
            return group;
        }

        /// <summary>
        /// key为一组中第一个被记录下的那个值，使用这个值便于使用时查找。
        /// </summary>
        /// <param name="basekvs"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        static string gen_base_metatable(Dictionary<string, string> basekvs, string key)
        {
            StringBuilder sb = new StringBuilder(string.Format("[{0}] = {", key));
            int n = basekvs.Count;
            foreach(var item in basekvs)
            {
                n--;
                sb.AppendFormat("{0} = {1}", item.Key, item.Value);
                if (n > 0)
                    sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        static string gen_group_metatable(List<config> configs)
        {
            StringBuilder sb = new StringBuilder("{");
            for(int i = 0; i < configs.Count; i++)
            {
                sb.Append(configs[i].key);
                if (i < configs.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        static string gen_group(table table, Dictionary<string, List<config>> group)
        {
            Dictionary<string, Dictionary<string, string>> basedic = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<List<config>, Dictionary<string, string>> groupdic = new Dictionary<List<config>, Dictionary<string, string>>();
            foreach (var list in group.Values)
            {
                if (list.Count > 0)
                {
                    Dictionary<string, string> basekvs = get_basekvs(list);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (basedic.ContainsKey(list[i].key))
                            Console.Error.WriteLine("已存在的key = " + list[i].key);
                        else
                            basedic.Add(list[i].key, basekvs);
                    }
                    groupdic.Add(list, basekvs);
                }
            }

            //去除不需要生成的属性
            property temp;
            Dictionary<string, string> basekv = new Dictionary<string, string>();
            for (int i = 0; i < table.configs.Count; i++)
            {
                if (group.ContainsKey(table.configs[i].key))
                {
                    basekv = basedic[table.configs[i].key];
                    for (int j = 0; j < table.configs[i].properties.Count; j++)
                    {
                        temp = table.configs[i].properties[j];
                        if (basekv.ContainsKey(temp.name) && basekv[temp.name] == temp.value)
                            table.configs.RemoveAt(j);
                    }
                }
            }

            Func<string> genmeta = () =>
            {
                StringBuilder __base = new StringBuilder("local __base = {\n");
                StringBuilder __groups = new StringBuilder("local groups = {\n");
                int n = groupdic.Count;
                foreach (var item in groupdic)
                {
                    n--;
                    __groups.Append(gen_group_metatable(item.Key));
                    __base.Append(gen_base_metatable(item.Value, item.Key[0].key));
                    if(n > 0)
                    {
                        __groups.Append(",\n");
                        __base.Append(",\n");
                    }
                    else
                    {
                        __groups.Append("\n");
                        __base.Append("\n");
                    }
                }
                __base.Append("for _,v in pairs(__base) do\n\tv.__index = v\nend\n");
                __groups.Append("for i=1, #groups do\n\tfor j=1, #groups[i] do\n\t\tsetmetatable(%s[groups[i][j]], __base[groups[i][1]])\n\tend\nend\n");
                return __base.ToString() + __groups.ToString();
            };
            return table.GenString(genmeta);
        }

        static string optimal_group(table table, Setting setting)
        {
            Dictionary<string, List<config>> group = partition_grop(table, setting.EigenValues);

            return string.Empty;
        }
        #endregion
    }
}
