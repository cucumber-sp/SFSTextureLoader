using ModLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SFS;
using SFS.IO;
using SFS.Parts;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TextureLoader
{
    public class Main : SFSMod
    {
        public class T2DConverter : JsonConverter<Texture2D>
        {
            public override Texture2D ReadJson(JsonReader reader, Type objectType, Texture2D existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override void WriteJson(JsonWriter writer, Texture2D value, JsonSerializer serializer)
            {
                writer.WriteValue(value.name);
            }
        }

        public Main() : base("TextureLoader", "TextureLoader", "Cucumber Space", "v1.1.x", "v0.1")
        {
        }

        public override void load()
        {
            Helper.OnBuildSceneLoaded += Helper_OnBuildSceneLoaded;
        }

        private void Helper_OnBuildSceneLoaded(object sender, EventArgs _)
        {
            Debug.Log("Thanks Exund for help.");
            foreach (FolderPath path in FileLocations.BaseFolder.Extend("TextureLoader").Extend("ColorTextures").GetFoldersInFolder(false))
            {

                try
                {
                    string configString = File.ReadAllText(path.Clone().Extend("config.txt"));
                    JObject configJson = JObject.Parse(configString);

                    if (!Base.partsLoader.colorTextures.ContainsKey(configJson["name"].ToObject<string>()))
                    {
                        ColorTexture myColorTexture = ScriptableObject.CreateInstance<ColorTexture>();
                        myColorTexture.tags = configJson["tags"].ToObject<string[]>();

                        var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
                        {
                            MaxDepth = 10,
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            Converters = new List<JsonConverter>()
                        {
                            new StringEnumConverter()
                            {
                                AllowIntegerValues = true
                            },
                            new T2DConverter()
                        }
                        });

                        var colorTexJson = (JObject)configJson["colorTex"];

                        PartTexture myPartTexture = colorTexJson.ToObject<PartTexture>(serializer);

                        Texture2D forIcon = null;

                        var myTextures = new List<PerValueTexture>();
                        foreach (JToken textureToken in colorTexJson["textures"].ToObject<JArray>())
                        {
                            JObject texture = textureToken.ToObject<JObject>();

                            PerValueTexture perValueTexture = new PerValueTexture
                            {
                                ideal = texture["ideal"].ToObject<float>()
                            };

                            Texture2D texture2D = new Texture2D(2, 2);
                            texture2D.LoadImage(File.ReadAllBytes(path.Clone().Extend(texture["texture"].ToObject<string>())));
                            texture2D.Apply();

                            perValueTexture.texture = texture2D;
                            myTextures.Add(perValueTexture);

                            if (!forIcon)
                            {
                                forIcon = texture2D;
                            }
                        }
                        forIcon = (forIcon ?? Texture2D.redTexture);
                        myPartTexture.textures = myTextures.ToArray();
                        myPartTexture.icon = Sprite.Create(forIcon, new Rect(0, 0, forIcon.width, forIcon.height), new Vector2(0.5f, 0.5f));

                        myColorTexture.colorTex = myPartTexture;
                        myColorTexture.name = configJson["name"].ToObject<string>();

                        Base.partsLoader.colorTextures.Add(myColorTexture.name, myColorTexture);

                        Debug.Log(myColorTexture.name + " was loaded");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to load: " + path);
                    Debug.Log(e);
                }
            }
        }

        public override void unload()
        {

        }
    }
}
