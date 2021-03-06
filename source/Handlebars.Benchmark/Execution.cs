using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.CompileFast;
using Newtonsoft.Json.Linq;
using BenchmarkDotNet.Jobs;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class Execution
    {
        [Params(2, 5, 10)]
        public int N;

        [Params("current", "current-cache", "current-fast", "current-fast-cache", "1.10.1")]
        public string Version;

        [Params("object", "dictionary")]
        public string DataType;
        
        private Func<object, string>[] _templates;
        
        private object _data;

        [GlobalSetup]
        public void Setup()
        {
            const string template = @"
                childCount={{level1.Count}}
                childCount2={{level1.Count}}
                {{#each level1}}
                    id={{id}}
                    childCount={{level2.Count}}
                    childCount2={{level2.Count}}
                    index=[{{@../../index}}:{{@../index}}:{{@index}}]
                    first=[{{@../../first}}:{{@../first}}:{{@first}}]
                    last=[{{@../../last}}:{{@../last}}:{{@last}}]
                    {{#each level2}}
                        id={{id}}
                        childCount={{level3.Count}}
                        childCount2={{level3.Count}}
                        index=[{{@../../index}}:{{@../index}}:{{@index}}]
                        first=[{{@../../first}}:{{@../first}}:{{@first}}]
                        last=[{{@../../last}}:{{@../last}}:{{@last}}]
                        {{#each level3}}
                            id={{id}}
                            index=[{{@../../index}}:{{@../index}}:{{@index}}]
                            first=[{{@../../first}}:{{@../first}}:{{@first}}]
                            last=[{{@../../last}}:{{@../last}}:{{@last}}]
                        {{/each}}
                    {{/each}}    
                {{/each}}";

            switch (DataType)
            {
                case "object":
                    _data = new { level1 = ObjectLevel1Generator()};
                    break;
                
                case "dictionary":
                    _data = new Dictionary<string, object>(){ ["level1"] = DictionaryLevel1Generator()};
                    break;
                
                case "json":
                    _data = new JObject {["level1"] = JsonLevel1Generator()};
                    break;
            }
            
            if (!Version.StartsWith("current"))
            {
                var assembly = Assembly.LoadFile($"{Environment.CurrentDirectory}\\PreviousVersion\\{Version}.dll");
                var type = assembly.GetTypes().FirstOrDefault(o => o.Name == nameof(Handlebars));
                var methodInfo = type.GetMethod("Create");
                var handlebars = methodInfo.Invoke(null, new object[]{ null });
                var objType = handlebars.GetType();

                var compileMethod = objType.GetMethod("Compile", new[]{typeof(string)});
                _templates = new[]
                {
                    (Func<object, string>) compileMethod.Invoke(handlebars, new []{template}),
                };
            }
            else
            {
                Handlebars.Configuration.CompileTimeConfiguration.UseAggressiveCaching = Version.Contains("cache");
                
                if (Version.Contains("fast"))
                {
                    Handlebars.Configuration.UseCompileFast();
                }
                
                _templates = new[]
                {
                    Handlebars.Compile(template)
                };
            }
            
            List<object> ObjectLevel1Generator()
            {
                var level = new List<object>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new
                    {
                        id = $"{i}",
                        level2 = ObjectLevel2Generator(i)
                    });
                }

                return level;
            }
            
            List<object> ObjectLevel2Generator(int id1)
            {
                var level = new List<object>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new
                    {
                        id = $"{id1}-{i}",
                        level3 = ObjectLevel3Generator(id1, i)
                    });
                }

                return level;
            }
            
            List<object> ObjectLevel3Generator(int id1, int id2)
            {
                var level = new List<object>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new
                    {
                        id = $"{id1}-{id2}-{i}"
                    });
                }

                return level;
            }

            List<Dictionary<string, object>> DictionaryLevel1Generator()
            {
                var level = new List<Dictionary<string, object>>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new Dictionary<string, object>()
                    {
                        ["id"] = $"{i}",
                        ["level2"] = DictionaryLevel2Generator(i)
                    });
                }

                return level;
            }
            
            List<Dictionary<string, object>> DictionaryLevel2Generator(int id1)
            {
                var level = new List<Dictionary<string, object>>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new Dictionary<string, object>()
                    {
                        ["id"] = $"{id1}-{i}",
                        ["level3"] = DictionaryLevel3Generator(id1, i)
                    });
                }

                return level;
            }
            
            List<Dictionary<string, object>> DictionaryLevel3Generator(int id1, int id2)
            {
                var level = new List<Dictionary<string, object>>();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new Dictionary<string, object>()
                    {
                        ["id"] = $"{id1}-{id2}-{i}"
                    });
                }

                return level;
            }

            JArray JsonLevel1Generator()
            {
                var level = new JArray();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new JObject
                    {
                        ["id"] = $"{i}",
                        ["level2"] = JsonLevel2Generator(i)
                    });
                }

                return level;
            }
            
            JArray JsonLevel2Generator(int id1)
            {
                var level = new JArray();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new JObject
                    {
                        ["id"] = $"{id1}-{i}",
                        ["level3"] = JsonLevel3Generator(id1, i)
                    });
                }

                return level;
            }
            
            JArray JsonLevel3Generator(int id1, int id2)
            {
                var level = new JArray();
                for (int i = 0; i < N; i++)
                {
                    level.Add(new JObject()
                    {
                        ["id"] = $"{id1}-{id2}-{i}"
                    });
                }

                return level;
            }
        }

        [Benchmark]
        public string WithParentIndex()
        {
            return _templates[0].Invoke(_data);
        }
    }
}