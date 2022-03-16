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
using System.Linq;
using UnityEngine;

namespace TextureLoader
{
    public class Main : SFSMod
    {
        private static Dictionary<string, ShadowTexture> shadowTextures;

        public class T2DConverter : JsonConverter<Texture2D>
        {
            public override Texture2D ReadJson(JsonReader reader, Type objectType, Texture2D existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override void WriteJson(JsonWriter writer, Texture2D value, JsonSerializer serializer)
            {
                writer.WriteValue(value.name + ".png");
            }
        }

        public Main() : base("TextureLoader", "TextureLoader", "Cucumber Space", "v1.1.x", "v0.1")
        {
        }

        public override void load()
        {
            Debug.Log("Thanks Exund for help.");

            var colorTextures = Base.partsLoader.colorTextures;
            var shapeTextures = Base.partsLoader.shapeTextures;

            var TextureLoaderFolder = FileLocations.BaseFolder.CloneAndExtend("TextureLoader").CreateFolder();
            var ColorTexturesFolder = TextureLoaderFolder.CloneAndExtend("ColorTextures").CreateFolder();
            var ShadowTexturesFolder = TextureLoaderFolder.CloneAndExtend("ShadowTextures").CreateFolder();
            var ShapeTexturesFolder = TextureLoaderFolder.CloneAndExtend("ShapeTextures").CreateFolder();



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

            foreach (FolderPath path in ColorTexturesFolder.GetFoldersInFolder(false))
            {
                try
                {
                    string configString = File.ReadAllText(path.CloneAndExtend("config.txt"));
                    JObject configJson = JObject.Parse(configString);
                    var name = configJson["name"].ToString();

                    if (!colorTextures.ContainsKey(name))
                    {
                        var colorTexture = ScriptableObject.CreateInstance<ColorTexture>();
                        colorTexture.name = name;
                        colorTexture.tags = configJson["tags"].ToObject<string[]>();

                        var colorTexJson = (JObject)configJson["colorTex"];
                        var partTexture = CreatePartTexture(path, colorTexJson, serializer);
                        colorTexture.colorTex = partTexture;

                        colorTextures.Add(colorTexture.name, colorTexture);

                        Debug.Log($"ColorTexture \"{name}\" was loaded");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to load: " + path);
                    Debug.Log(e);
                }
            }

            shadowTextures = Resources.FindObjectsOfTypeAll<ShadowTexture>().ToDictionary((s) => s.name, (s) => s);

            foreach (FolderPath path in ShadowTexturesFolder.GetFoldersInFolder(false))
            {
                try
                {
                    string configString = File.ReadAllText(path.CloneAndExtend("config.txt"));
                    JObject configJson = JObject.Parse(configString);
                    var name = configJson["name"].ToString();

                    if (!colorTextures.ContainsKey(name))
                    {
                        var shadowTexture = ScriptableObject.CreateInstance<ShadowTexture>();
                        shadowTexture.name = name;

                        var shadowTexJson = (JObject)configJson["texture"];
                        var partTexture = CreatePartTexture(path, shadowTexJson, serializer);
                        shadowTexture.texture = partTexture;

                        shadowTextures.Add(shadowTexture.name, shadowTexture);

                        Debug.Log($"ShadowTexture \"{name}\" was loaded");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to load: " + path);
                    Debug.Log(e);
                }
            }

            foreach (FolderPath path in ShapeTexturesFolder.GetFoldersInFolder(false))
            {
                try
                {
                    string configString = File.ReadAllText(path.CloneAndExtend("config.txt"));
                    JObject configJson = JObject.Parse(configString);
                    var name = configJson["name"].ToString();

                    if (!colorTextures.ContainsKey(name))
                    {
                        var shapeTexture = ScriptableObject.CreateInstance<ShapeTexture>();
                        shapeTexture.name = name;
                        shapeTexture.tags = configJson["tags"].ToObject<string[]>();

                        var shadowTex = configJson["shadowTex"].ToString();
                        if (!shadowTextures.TryGetValue(shadowTex, out shapeTexture.shadowTex))
                        {
                            Debug.Log($"Failed to find ShadowTexture \"{shadowTex}\" for ShapeTexture \"{name}\"");
                        }

                        var shapeTexJson = (JObject)configJson["shapeTex"];
                        var partTexture = CreatePartTexture(path, shapeTexJson, serializer);
                        shapeTexture.shapeTex = partTexture;
                        
                        shapeTextures.Add(shapeTexture.name, shapeTexture);

                        Debug.Log($"ShadowTexture \"{name}\" was loaded");
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

        PartTexture CreatePartTexture(FolderPath path, JObject json, JsonSerializer serializer)
        {
            var partTexture = json.ToObject<PartTexture>(serializer);

            Texture2D forIcon = null;

            var textures = new List<PerValueTexture>();
            foreach (JObject texture in json["textures"])
            {
                var perValueTexture = new PerValueTexture
                {
                    ideal = texture["ideal"].ToObject<float>()
                };

                var texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(File.ReadAllBytes(path.CloneAndExtend(texture["texture"].ToString())));
                texture2D.Apply();

                perValueTexture.texture = texture2D;
                textures.Add(perValueTexture);

                if (!forIcon)
                {
                    forIcon = texture2D;
                }
            }

            forIcon = forIcon ?? Texture2D.redTexture;
            partTexture.textures = textures.ToArray();
            partTexture.icon = Sprite.Create(forIcon, new Rect(0, 0, forIcon.width, forIcon.height), new Vector2(0.5f, 0.5f));

            return partTexture;
        }
    }
}
