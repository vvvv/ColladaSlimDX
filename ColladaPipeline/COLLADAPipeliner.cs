///*
// * Copyright 2006 Sony Computer Entertainment Inc.
// * 
// * Licensed under the SCEA Shared Source License, Version 1.0 (the "License"); you may not use this 
// * file except in compliance with the License. You may obtain a copy of the License at:
// * http://research.scea.com/scea_shared_source_license.html
// *
// * Unless required by applicable law or agreed to in writing, software distributed under the License 
// * is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
// * implied. See the License for the specific language governing permissions and limitations under the 
// * License.
// */
//
//#region Using Statements
//using System;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;
///*
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Audio;
//using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using Microsoft.Xna.Framework.Storage;
//
//using Microsoft.Xna.Framework.Content.Pipeline; // ContentImporter
//using Microsoft.Xna.Framework.Content.Pipeline.Processors; // ModelProcessor
//using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler; // ContentTypeWriter
//using Microsoft.Xna.Framework.Content.Pipeline.Graphics; // MeshBuilder
//*/
//using SlimDX.Direct3D9;
//
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters.Binary;  // serialization
//
//using VVVV.Utils.Collada.ColladaDocument;
//
//#endregion
//
//
//namespace VVVV.Utils.Collada.ColladaPipeline
//{
//
//      public class Processor
//      {
//        /// <summary>
//        /// A context passed into all the COLLADA Processor functions. 
//        /// Contains the current document, the materialTable and binding information.
//        /// </summary>
//        public class Context
//        {
//            public Document doc;
//            public Dictionary<string, Material> materialTable;
//            public Dictionary<string, string> materialBinding;
//            public Dictionary<uint, uint> textureCoordinateBinding;
//            public Dictionary<string, Dictionary<string, uint>> textureBindings;
//        }
//        /// <summary>
//        /// Resolve the material binding  for the given "<instance_naterial>".
//        /// <param name="instance">The "<instance_material>" element we need to resolve the binding for.</param>
//        /// <param name="context"> The current context for the COLLADA Processor</param>
//        /// </summary>
//        static public void resolveBinding(
//            Document.InstanceWithMaterialBind instance,
//            Context context)
//        {
//            foreach (DictionaryEntry de in (IDictionary)(instance.bindMaterial.instanceMaterials))
//            {
//                // material binding
//                context.materialBinding[((Document.InstanceMaterial)de.Value).symbol] = (string)de.Key;
//                if (((Document.InstanceMaterial)de.Value).bindVertexInputs != null) // textureset binding 
//                    foreach (Document.InstanceMaterial.BindVertexInput bindVertexInput in ((Document.InstanceMaterial)de.Value).bindVertexInputs)
//                    {
//                        if (context.textureBindings[(string)de.Key].ContainsKey(bindVertexInput.semantic))
//                        {
//                            uint tmp = context.textureBindings[(string)de.Key][bindVertexInput.semantic];
//                            uint tmp2 = bindVertexInput.inputSet;
//                            context.textureCoordinateBinding[tmp2] = tmp;
//                        }
//                    }
//                else if (((Document.InstanceMaterial)de.Value).binds != null)
//                    foreach (Document.InstanceMaterial.Bind bind in ((Document.InstanceMaterial)de.Value).binds)
//                    {
//                        uint tmp = context.textureBindings[(string)de.Key][bind.semantic];
//                        // assuming only one texture coordinate set
//                        context.textureCoordinateBinding[0] = tmp;
//                    }
//            }
//        }
//        /// <summary>
//        /// Create a BasicMaterialContent from a "<material>".
//        /// <param name="material">The "<material>" element to be converted.</param>
//        /// <param name="context"> The current context for the COLLADA Processor</param>
//        /// </summary>
//        static public Material ReadMaterial(
//            Document.Material material,
//            Context context)
//        {
//            uint textureChannel = 0;
//            Dictionary<string, uint> textureBinding = context.textureBindings[material.id];
//            BasicMaterialContent materialContent = new BasicMaterialContent();
//            Document.Effect effect = (Document.Effect)context.doc.dic[material.instanceEffect.Fragment];
//
//            if (effect == null) throw new Exception("cannot find effect#" + material.instanceEffect.Fragment);
//            // search common profile with correct asset....
//            Document.ProfileCOMMON profile;
//            foreach (Document.IProfile tmpProfile in effect.profiles)
//            {
//                if (tmpProfile is Document.ProfileCOMMON)
//                {
//                    profile = (Document.ProfileCOMMON)tmpProfile;
//                    goto Found;
//                }
//            }
//            throw new Exception("Could not find profile_COMMON in effect" + effect.ToString());
//        Found:
//            // read params
//            Dictionary<string, string> samplerBind = new Dictionary<string, string>();
//            Dictionary<string, string> imageBind = new Dictionary<string, string>();
//
//            // Read Technique
//            Document.SimpleShader shader = ((Document.ProfileCOMMON)profile).technique.shader;
//
//            // BasicShader only accept texture for the diffuse channel
//            if (shader.diffuse is Document.Texture)
//            {
//                string sampler = ((Document.Texture)shader.diffuse).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)context.doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.diffuse).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//            }
//            else if (shader.diffuse is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.diffuse;
//                // TODO: manage color[3] in transparency
//                materialContent.DiffuseColor = new Vector3(color[0], color[1], color[2]);
//            }
//            if (shader.ambient is Document.Texture)
//            {
//                // Basic Material does not accept texture on ambient channel
//                /*
//                string sampler = ((Document.Texture)shader.ambient).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.ambient).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                */
//
//            }
//            else if (shader.ambient is Document.Color)
//            {
//                // XNA BasicMaterial has no ambient 
//            }
//            if (shader.emission is Document.Texture)
//            {
//                // XNA BasicMaterial does not accept texture for emmision
//                /*
//                string sampler = ((Document.Texture)shader.emission).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.emission).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.emission is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.emission;
//                materialContent.EmissiveColor = new Vector3(color[0], color[1], color[2]);
//            }
//            if (shader.specular is Document.Texture)
//            {
//                // XNA BasicMaterial does not accept texture for specular
//                /*
//                string sampler = ((Document.Texture)shader.specular).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.specular).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.specular is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.specular;
//                materialContent.SpecularColor = new Vector3(color[0], color[1], color[2]);
//                if (shader.shininess is Document.Float)
//                    materialContent.SpecularPower = ((Document.Float)shader.shininess).theFloat;
//            }
//            if (shader.transparency is Document.Texture)
//            {
//                // XNA Basic Shader does not accept a texture for the transparency channel
//                /*
//                string sampler = ((Document.Texture)shader.transparency).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.transparency).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.transparency is Document.Float)
//            {
//                materialContent.Alpha = ((Document.Float)shader.transparency).theFloat;
//            }
//
//            return materialContent;
//        }
//        /// <summary>
//        /// Create a BasicEffect from a "<material>".
//        /// <param name="material">The "<material>" element to be converted.</param>
//        /// <param name="context"> The current context for the COLLADA Processor</param>
//        /// </summary>
//        static public Effect MaterialToEffect(
//            Device graphicsDevice,
//            Document.Material material,
//            Context context)
//        {
//            uint textureChannel = 0;
//            Dictionary<string, uint> textureBinding = context.textureBindings[material.id];
//            BasicEffect basicEffect = new BasicEffect(graphicsDevice, null);
//            Document.Effect effect = (Document.Effect)context.doc.dic[material.instanceEffect.Fragment];
//
//            if (effect == null) throw new Exception("cannot find effect#" + material.instanceEffect.Fragment);
//            // search common profile with correct asset....
//            Document.ProfileCOMMON profile;
//            foreach (Document.IProfile tmpProfile in effect.profiles)
//            {
//                if (tmpProfile is Document.ProfileCOMMON)
//                {
//                    profile = (Document.ProfileCOMMON)tmpProfile;
//                    goto Found;
//                }
//            }
//            throw new Exception("Could not find profile_COMMON in effect" + effect.ToString());
//        Found:
//            // read params
//            Dictionary<string, string> samplerBind = new Dictionary<string, string>();
//            Dictionary<string, string> imageBind = new Dictionary<string, string>();
//
//            // Read Technique
//            Document.SimpleShader shader = ((Document.ProfileCOMMON)profile).technique.shader;
//
//            // BasicShader only accept texture for the diffuse channel
//            if (shader.diffuse is Document.Texture)
//            {
//                string sampler = ((Document.Texture)shader.diffuse).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)context.doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.diffuse).texcoord] = textureChannel++;
//                //basicEffect.Texture = new ExternalReference<TextureContent>(imagePath);
//                //basicEffect.Texture = new Texture2D(graphicsDevice, width, height, numberlevels, ResourceUsage.WriteOnly, SurfaceFormat.Rgba32);
//            }
//            else if (shader.diffuse is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.diffuse;
//                // TODO: manage color[3] in transparency
//                basicEffect.DiffuseColor = new Vector3(color[0], color[1], color[2]);
//            }
//            if (shader.ambient is Document.Texture)
//            {
//                // Basic Material does not accept texture on ambient channel
//                /*
//                string sampler = ((Document.Texture)shader.ambient).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.ambient).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                */
//
//            }
//            else if (shader.ambient is Document.Color)
//            {
//                // XNA BasicMaterial has no ambient 
//            }
//            if (shader.emission is Document.Texture)
//            {
//                // XNA BasicMaterial does not accept texture for emmision
//                /*
//                string sampler = ((Document.Texture)shader.emission).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.emission).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.emission is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.emission;
//                basicEffect.EmissiveColor = new Vector3(color[0], color[1], color[2]);
//            }
//            if (shader.specular is Document.Texture)
//            {
//                // XNA BasicMaterial does not accept texture for specular
//                /*
//                string sampler = ((Document.Texture)shader.specular).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.specular).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.specular is Document.Color)
//            {
//                Document.Color color = (Document.Color)shader.specular;
//                basicEffect.SpecularColor = new Vector3(color[0], color[1], color[2]);
//                if (shader.shininess is Document.Float)
//                    basicEffect.SpecularPower = ((Document.Float)shader.shininess).theFloat;
//            }
//            if (shader.transparency is Document.Texture)
//            {
//                // XNA Basic Shader does not accept a texture for the transparency channel
//                /*
//                string sampler = ((Document.Texture)shader.transparency).texture;
//                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
//                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
//                // now find image
//                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
//                // here associate 1 texture binding per texture in material
//                textureBinding[((Document.Texture)shader.transparency).texcoord] = textureChannel++;
//                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
//                 */
//            }
//            else if (shader.transparency is Document.Float)
//            {
//                basicEffect.Alpha = ((Document.Float)shader.transparency).theFloat;
//            }
//
//            return basicEffect;
//        }
//        /// <summary>
//        /// Parse the COLLADA node graph and create appropriate NodeContent, including transforms.
//        /// <param name="node">The "<node>" element to be converted.</param>
//        /// <param name="context"> The current context for the COLLADA Processor</param>
//        /// </summary>
//        static public NodeContent ReadNode(
//            Document.Node node,
//            Context context)
//        {
//            NodeContent content = null;
//            
//            if (node.instances != null)
//                foreach (Document.Instance instance in node.instances)
//                {
//                    if (instance is Document.InstanceGeometry)
//                    {
//                        // resolve bindings
//                        // MaterialBinding contails the material_id bind to each symbol in the <mesh>
//                        context.materialBinding = new Dictionary<string, string>();
//                        // TextureCoordinateBinding contains XNA mesh channel number for a given texcoord set#
//                        context.textureCoordinateBinding = new Dictionary<uint, uint>();
//                        resolveBinding((Document.InstanceWithMaterialBind)instance, context);
//
//                        Document.Geometry geo = (Document.Geometry)context.doc.dic[instance.url.Fragment];
//                        content = ReadGeometry(geo, context);
//                        content.Name = node.name;
//
//                    } else if (instance is Document.InstanceCamera)
//                    {
//                        // TODO: camera
//                        content = new NodeContent();
//                        content.Name = node.name;
//                    } else if (instance is Document.InstanceLight)
//                    {
//                        // TODO: light
//                        content = new NodeContent();
//                        content.Name = node.name;
//                    } else if (instance is Document.InstanceController)
//                    {
//
//                        Document.ISkinOrMorph skinOrMorph = ((Document.Controller)context.doc.dic[((Document.InstanceController)instance).url.Fragment]).controller;
//                        if (skinOrMorph is Document.Skin)
//                        {
//                            // XNA has no support for skining ?
//                            // get the pose model, and convert it to a static mesh for now
//
//                            // resolve bindings
//                            // MaterialBinding contails the material_id bind to each symbol in the <mesh>
//                            context.materialBinding = new Dictionary<string, string>();
//                            // TextureCoordinateBinding contains XNA mesh channel number for a given texcoord set#
//                            context.textureCoordinateBinding = new Dictionary<uint, uint>();
//                            resolveBinding((Document.InstanceWithMaterialBind)instance, context);
//
//                            Document.Geometry geo = ((Document.Geometry)context.doc.dic[((Document.Skin)skinOrMorph).source.Fragment]);
//                            content = ReadGeometry(geo, context);
//                            content.Name = node.name;
//                        } else if (skinOrMorph is Document.Morph)
//                        {
//                            // TODO: morph
//                            content = new NodeContent();
//                            content.Name = node.name;
//
//                        } else
//                            throw new Exception("Unknowned type of controler:" + skinOrMorph.GetType().ToString());
//                    }
//                    else if (instance is Document.InstanceNode)
//                    {
//                        Document.Node instanceNode = ((Document.Node)context.doc.dic[instance.url.Fragment]);
//                        content = ReadNode(instanceNode, context);
//                        content.Name = node.name;
//
//                    }
//                    else
//                        throw new Exception("Unkowned type of INode in scene :" + instance.GetType().ToString());
//                }
//
//
//            if (content == null) content = new NodeContent();
//            
//            // read transforms
//            content.Transform = Matrix.Identity;
//            
//            if (node.transforms != null)
//                foreach (Document.TransformNode transform in node.transforms)
//                {
//                    
//                    if (transform is Document.Translate)
//                        content.Transform = Matrix.CreateTranslation(transform[0], transform[1], transform[2]) * content.Transform;
//                    else if (transform is Document.Rotate) 
//                        content.Transform = Matrix.CreateFromAxisAngle(new Vector3(transform[0], transform[1], transform[2]), MathHelper.ToRadians(transform[3])) * content.Transform;
//                    else if (transform is Document.Lookat)
//                        content.Transform = Matrix.CreateLookAt(new Vector3(transform[0], transform[1], transform[2]), new Vector3(transform[3], transform[4], transform[5]), new Vector3(transform[6], transform[7], transform[8])) * content.Transform;
//                    else if (transform is Document.Matrix)
//                        content.Transform = new Matrix(transform[0], transform[1], transform[2], transform[3],
//                                                        transform[4], transform[5], transform[6], transform[7],
//                                                        transform[8], transform[9], transform[10], transform[11],
//                                                        transform[12], transform[13], transform[14], transform[15]) * content.Transform;
//                    else if (transform is Document.Scale)
//                        content.Transform = Matrix.CreateScale(transform[0], transform[1], transform[2]) * content.Transform;
//                    else if (transform is Document.Skew)
//                    {
//                        // Convert Skew to a matrix
//                        float angle = MathHelper.ToRadians(transform[0]);
//                        Vector3 a = new Vector3(transform[1], transform[2], transform[3]);
//                        Vector3 b = new Vector3(transform[4], transform[5], transform[6]);
//                        Vector3 n2 = Vector3.Normalize(b);
//                        Vector3 a1 = n2*Vector3.Dot(a,n2);
//                        Vector3 a2 = a-a1;
//                        Vector3 n1 = Vector3.Normalize(a2);
//                        float an1=Vector3.Dot(a,n1);
//                        float an2=Vector3.Dot(a,n2);
//                        double rx=an1*Math.Cos(angle) - an2*Math.Sin(angle);
//                        double ry=an1*Math.Sin(angle) + an2*Math.Cos(angle);
//                        float alpha = 0.0f;
//                        Matrix m = Matrix.Identity;
//
//                        if (rx <= 0.0) throw new Exception("Skew: angle too large");
//                        if (an1!=0) alpha=(float)(ry/rx-an2/an1);
//
//                        m.M11 = n1.X * n2.X * alpha + 1.0f;
//                        m.M12 = n1.Y * n2.X * alpha;
//                        m.M13 = n1.Z * n2.X * alpha;
//
//                        m.M21 = n1.X * n2.Y * alpha;
//                        m.M22 = n1.Y * n2.Y * alpha + 1.0f;
//                        m.M23 = n1.Z * n2.Y * alpha;
//
//                        m.M31 = n1.X * n2.Z * alpha;
//                        m.M32 = n1.Y * n2.Z * alpha;
//                        m.M33 = n1.Z * n2.Z * alpha + 1.0f;
//
//                        content.Transform = m * content.Transform;
//
//                    }
//                }
//                     
//            
//            if (node.children != null)
//                foreach (Document.Node child in node.children)
//                {
//                    content.Children.Add(ReadNode(child, context));
//                }
//
//            return content;
//
//        }
// 
//        /// <summary>
//        /// Convert a TRIANGLE based "<geometry>" in a MeshContent
//        /// <param name="geo">The "<geometry>" element to be converted.</param>
//        /// <param name="context"> The current context for the COLLADA Processor</param>
//        /// </summary>
//        static public MeshContent ReadGeometry(Document.Geometry geo, Context context)
//        {
//            Document.Input positionInput = null;
//            List<int> normals = new List<int>();
//            List<Document.Input> normalInputs = new List<Document.Input>();
//            int normalCount = 0;
//            List<int> textures = new List<int>();
//            List<Document.Input> textureInputs = new List<Document.Input>();
//            int textureCount = 0;
//            List<int> colors = new List<int>();
//            List<Document.Input> colorInputs = new List<Document.Input>();
//            int colorCount = 0;
//            List<int> tangents = new List<int>();
//            List<Document.Input> tangentInputs = new List<Document.Input>();
//            int tangentCount = 0;
//            List<int> binormals = new List<int>();
//            List<Document.Input> binormalInputs = new List<Document.Input>();
//            int binormalCount = 0;
//            List<int> userData = new List<int>();
//            List<Document.Input> userDataInputs = new List<Document.Input>();
//            int userDataCount = 0;
//
//            int i, j, k;
//
//            // Note: XNA meshbuilder does not allow for creating meshes that have different channel information
//            // so all the primitives in the COLLADA mesh must have the same number of channel for the following to work
//
//            MeshBuilder builder = MeshBuilder.StartMesh(geo.id);
//            positionInput = Util.GetPositionInput(geo.mesh);
//            builder.SwapWindingOrder = true;
//            int positionCount = ((Document.Source)positionInput.source).accessor.count;
//            for (i = 0; i < positionCount; i++)
//            {
//                float[] values = Util.GetSourceElement(context.doc,positionInput, i);
//                builder.CreatePosition(new Vector3(values[0],values[1],values[2]));
//            }
//            string usage;
//            bool vertexChannelsCreated = false;
//            foreach (Document.Primitive primitive in geo.mesh.primitives)
//            {
//                if (!(primitive is Document.Triangle))
//                    throw new Exception ("ReadGeometry only accept Triangle primitives");
//
//                // set primitive material from already resolved binding
//                if (context.materialTable != null)
//                    builder.SetMaterial(context.materialTable[context.materialBinding[primitive.material]]);
//
//                // reset counters for input arrays
//                normalCount = 0;
//                textureCount = 0;
//                colorCount = 0;
//                tangentCount = 0;
//                binormalCount = 0;
//                userDataCount = 0;
//
//                foreach (Document.Input input in Util.getAllInputs(context.doc,primitive))
//                {
//                    switch (input.semantic)
//                    {
//                        case "POSITION":
//                            positionInput = input;
//                            break;
//                        case "NORMAL":
//                            if (vertexChannelsCreated == false)
//                            {
//                                usage = VertexChannelNames.EncodeName("Normal", normalCount);
//                                normals.Add(builder.CreateVertexChannel<Vector3>(usage));
//                            }
//                            normalInputs.Add(input);
//                            normalCount++;
//                            break;
//                        case "TEXCOORD":
//                            if (context.textureCoordinateBinding != null)
//                            {
//                                uint inputSet = 0;
//                                if (input.set >= 0) inputSet = (uint)input.set;
//                                // if no binding can be found, then this text coord is not used.
//                                // XBA BuildMesh will complain if texcoords are there, but no texture is associated
//                                if (context.textureCoordinateBinding.ContainsKey(inputSet))
//                                {
//                                    uint usageSet = context.textureCoordinateBinding[inputSet];
//                                    if (vertexChannelsCreated == false)
//                                    {
//                                        usage = VertexChannelNames.EncodeName("TextureCoordinate", (int)usageSet);
//                                        textures.Add(builder.CreateVertexChannel<Vector2>(usage));
//                                    }
//                                    textureInputs.Add(input);
//                                    textureCount++;
//                                }
//                            }
//                            break;
//                        case "COLOR":
//                            if (vertexChannelsCreated == false)
//                            {
//                                usage = VertexChannelNames.EncodeName("Color", colorCount);
//                                colors.Add(builder.CreateVertexChannel<Vector3>(usage));
//                            }
//                            colorInputs.Add(input);
//                            colorCount++;
//                            // set material for enabling vertex color 
//                            ((BasicMaterialContent)context.materialTable[context.materialBinding[primitive.material]]).VertexColorEnabled = true;
//                            break;
//                        case "TANGENT":
//                            if (vertexChannelsCreated == false)
//                            {
//                                usage = VertexChannelNames.EncodeName("Tangent", tangentCount);
//                                tangents.Add(builder.CreateVertexChannel<Vector3>(usage));
//                            }
//                            tangentInputs.Add(input);
//                            tangentCount++;
//                            break;
//                        case "BINORMAL":
//                            if (vertexChannelsCreated == false)
//                            {
//                                usage = VertexChannelNames.EncodeName("Binormal", binormalCount);
//                                binormals.Add(builder.CreateVertexChannel<Vector3>(usage));
//                            }
//                            binormalInputs.Add(input);
//                            binormalCount++;
//                            break;
//                        case "UV":
//                            // where does user data go ?
//                            if (vertexChannelsCreated == false)
//                            {
//                                usage = VertexChannelNames.EncodeName("UV", userDataCount);
//                                userData.Add(builder.CreateVertexChannel<Vector2>(usage));
//                            }
//                            userDataInputs.Add(input);
//                            userDataCount++;
//                            break;
//                        default:
//                            throw new Exception("Primitive Channel Semantic " + input.semantic + " Not supported");
//                    }
//                }
//                vertexChannelsCreated = true;
//
//                for (i = 0; i < primitive.count * 3; i++)
//                {
//                    for (k = 0; k < normalCount; k++)
//                    {
//                        j = Util.GetPValue(normalInputs[k], primitive, i);
//                        float[] values = Util.GetSourceElement(context.doc,normalInputs[k], j);
//                        builder.SetVertexChannelData(normals[k], new Vector3(values[0],values[1],values[2]) );
//                    }
//                    for (k = 0; k < textureCount; k++)
//                    {
//                        j = Util.GetPValue(textureInputs[k], primitive, i);
//                        float[] values = Util.GetSourceElement(context.doc,textureInputs[k], j);
//                        if (values.Length==2)
//                        {
//                            // Reverse texture 'T' coordinate for Direct X.
//                            Vector2 v = new Vector2(values[0],values[1]);
//                            v.Y = 1.0f - v.Y;
//                            builder.SetVertexChannelData(textures[k], v);
//                        }
//                        else 
//                            builder.SetVertexChannelData(textures[k], new Vector3(values[0],values[1],values[2]));
//                    }
//                    for (k = 0; k < colorCount; k++)
//                    {
//                        j = Util.GetPValue(colorInputs[k], primitive, i);
//                        builder.SetVertexChannelData(colors[k], Util.GetSourceElement(context.doc,colorInputs[k],j));
//                    }
//                    for (k = 0; k < binormalCount; k++)
//                    {
//                        j = Util.GetPValue(binormalInputs[k], primitive, i);
//                        builder.SetVertexChannelData(binormals[k], Util.GetSourceElement(context.doc,binormalInputs[k],j));
//                    }
//                    for (k = 0; k < userDataCount; k++)
//                    {
//                        j = Util.GetPValue(userDataInputs[k], primitive, i);
//                        builder.SetVertexChannelData(userData[k], Util.GetSourceElement(context.doc,userDataInputs[k],j));
//                    }
//                    for (k = 0; k < tangentCount; k++)
//                    {
//                        j = Util.GetPValue(tangentInputs[k], primitive, i);
//                        builder.SetVertexChannelData(tangents[k], Util.GetSourceElement(context.doc,tangentInputs[k],j));
//                    }
//                    j = Util.GetPValue(positionInput, primitive, i);
//                    builder.AddTriangleVertex(j);
//                }
//            }
//            // Optimize resulting mesh for vizualisation
//            MeshContent mesh = builder.FinishMesh();
//            MeshHelper.MergeDuplicatePositions(mesh, 0);
//            MeshHelper.MergeDuplicateVertices(mesh);
//            MeshHelper.OptimizeForCache(mesh);
//
//            return mesh;
//        }
//        /// <summary>
//        /// Convert the "<visual_scene>" of a COLLADA document into a ModeContent.
//        /// Includes geometry, material and hierarchy
//        /// <param name="doc">The COLLADA document to be converted.</param>
//        /// </summary>
//        static public NodeContent Convert(Document doc)
//        {
//
//            string urlFrag = doc.instanceVisualScene.url.Fragment;
//            Document.VisualScene scene = (Document.VisualScene)doc.dic[urlFrag];
//            // Maybe we should load all the scenes from the scene library instead of only the instanced one ?
//            if (scene == null) throw new Exception("NO VISUAL SCENE IN DOCUMENT");
//
//            // Read materials
//            Context context = new Context(); ;
//            context.doc = doc;
//            context.materialTable = new Dictionary<String, MaterialContent>();
//            context.textureBindings = new Dictionary<string, Dictionary<string, uint>>();
//
//            foreach (Document.Material material in doc.materials)
//            {
//                // textureBindings contains the XNA mesh texture channel number for each texture binding target
//                context.textureBindings[material.id] = new Dictionary<string, uint>();
//                // materialTable contains the XNA material for each COLLADA material
//                context.materialTable[material.id] = ReadMaterial(material, context);
//            }
//
//            // recursive call to load scene
//            NodeContent content = new NodeContent();
//            content.Name = scene.name;
//
//            foreach (Document.Node node in scene.nodes)
//            {
//                content.Children.Add(ReadNode(node, context));
//            }
//            return content;
//        }
//    }
//
//
//    /// <summary>
//    /// The COLLADA Importer hook for the XNA content pipeline
//    /// Load the COLLADA document, then call the necessary condioners on it
//    /// </summary>
//    [ContentImporter(".dae", DisplayName = "COLLADA Importer - with conditioners")]
//    public class COLLADAImporter : ContentImporter<Document>
//    {
//        public override Document Import(string filename, ContentImporterContext context)
//        {
//            Document doc = new Document(filename);
//            Conditioner.ConvexTriangulator(doc);
//            Conditioner.Reindexor(doc);
//            return doc;
//        }
//    }
//
//    /// <summary>
//    /// The COLLADA Writer makes a serialized (binary) version of the COLLADA document.
//    /// This is the output of the first stage (import) of the XNA content pipeline.
//    /// </summary>
//    [ContentTypeWriter]
//    public class COLLADAWriter : ContentTypeWriter<Document>
//    {
//        protected override void Write(ContentWriter output, Document doc)
//        {
//            BinaryFormatter formatter = new BinaryFormatter();
//            formatter.Serialize(output.BaseStream, doc);
//        }
//        public override string GetRuntimeReader(TargetPlatform targetPlatform)
//        {
//
//            return typeof(COLLADAReader).AssemblyQualifiedName;
//        }
//        public override string GetRuntimeType(TargetPlatform targetPlatform)
//        {
//            return typeof(Document).AssemblyQualifiedName;
//        }
//    }
//    /// <summary>
//    /// The COLLADA Reader hook for the XNA content pipeline
//    /// This will load back the serialized COLLADA document, ready for the second stage of the XNA content pipeline
//    /// </summary>
//    public class COLLADAReader : ContentTypeReader<Document>
//    {
//        protected override Document Read(ContentReader input, Document existingInstance)
//        {
//
//            //document doc = new document();
//            BinaryFormatter formatter = new BinaryFormatter();
//            return (Document)formatter.Deserialize(input.BaseStream);
//
//        }
//    }
//
//    /// <summary>
//    /// The COLLADA Writer makes a serialized (binary) version of the COLLADA document.
//    /// </summary>
//    [ContentTypeWriter]
//    public class COLLADADocumentWriter : ContentTypeWriter<COLLADAModel>
//    {
//        protected override void Write(ContentWriter output, COLLADAModel doc)
//        {
//            BinaryFormatter formatter = new BinaryFormatter();
//            formatter.Serialize(output.BaseStream, doc);
//        }
//        public override string GetRuntimeReader(TargetPlatform targetPlatform)
//        {
//            return typeof(COLLADADocumentReader).AssemblyQualifiedName;
//        }
//        public override string GetRuntimeType(TargetPlatform targetPlatform)
//        {
//            return typeof(COLLADAModel).AssemblyQualifiedName;
//        }
//    }
//    /// <summary>
//    /// The COLLADA Reader hook for the XNA content pipeline
//    /// This will load back the serialized COLLADAModel, and create the vertex and index buffers,
//    /// so it is ready to be displayed by the application.
//    /// </summary>
//    public class COLLADADocumentReader : ContentTypeReader<COLLADAModel>
//    {
//        protected override COLLADAModel Read(ContentReader input, COLLADAModel existingInstance)
//        {
//            BinaryFormatter formatter = new BinaryFormatter();
//            COLLADAModel colladaModel = (COLLADAModel)formatter.Deserialize(input.BaseStream);
//            IGraphicsDeviceService graphicsDeviceService = (IGraphicsDeviceService)input.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
//            colladaModel.PostLoadingProcess(graphicsDeviceService.GraphicsDevice, input.ContentManager);
//
//            return colladaModel;
//        }
//    }
//    /// <summary>
//    /// COLLADAModel content processor
//    /// This returns a COLLADAModel ready to be used in the application 
//    /// </summary>
//    [ContentProcessor(DisplayName = "COLLADA -> COLLADAModel processor")]
//    public class COLLADAModelProcessor : ContentProcessor<Document, COLLADAModel>
//    {
//        public override COLLADAModel Process(Document doc, ContentProcessorContext context)
//        {
//            return new COLLADAModel(doc, context);
//        }
//    }
//     
//    /// <summary>
//    /// A simple COLLADA content processor
//    /// This will create XNA Model from the visual_scene of the collada document that was previously loaded and conditionned.
//    /// </summary>
//    [ContentProcessor(DisplayName = "COLLADA -> XNA Model processor")]
//    public class COLLADAProcessor : ContentProcessor<Document, ModelContent>
//    {
//        public override ModelContent Process(Document doc, ContentProcessorContext context)
//        {
//            NodeContent input = Processor.Convert(doc);
//            ModelProcessor modelProcessor = new ModelProcessor();
//            ModelContent content = modelProcessor.Process(input, context);
//            //context.Logger.LogWarning(null,input.Identity,message);
//            return content;
//        }
//    }
//
//    /// <summary>
//    /// A simple COLLADA content processor
//    /// This provides directly access to the COLLADA.Document 
//    /// </summary>
//    [ContentProcessor(DisplayName = "COLLADA Document processor - no op")]
//    public class COLLADAOUTProcessor : ContentProcessor<Document, Document>
//    {
//        public override Document Process(Document doc, ContentProcessorContext context)
//        {
//            return doc;
//        }
//    }
//}
