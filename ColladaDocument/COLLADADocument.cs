/*
 * Copyright 2006 Sony Computer Entertainment Inc.
 * 
 * Licensed under the SCEA Shared Source License, Version 1.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at:
 * http://research.scea.com/scea_shared_source_license.html
 *
 * Unless required by applicable law or agreed to in writing, software distributed under the License
 * is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 * implied. See the License for the specific language governing permissions and limitations under the
 * License.
 */

#region Using Statements
using System;
using System.Linq;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SlimDX;
using ColladaSlimDX.Utils;

// XmlDocument
// Console
// CultureInfo

// List

// Regex


#endregion


namespace ColladaSlimDX.ColladaDocument
{
    // Summary:
    //     Represents a COLLADA document

    [Serializable()]
    public class Document
    {
        protected bool initialized = false;
        [NonSerialized()]
        protected XmlNamespaceManager nsmgr = null;
        [NonSerialized()]
        protected XmlDocument colladaDocument = null;
        //[NonSerialized()]
        public Uri baseURI;
        public string documentName;
        public string filename;
        public Hashtable dic;
        [NonSerialized()]
        protected CultureInfo encoding;
        
        protected CoordinateSystem coordinateSystem;
        public CoordinateSystem CoordinateSystem
        {
            get
            {
                return coordinateSystem;
            }
        }

        // Helper functions

        public IColorOrTexture ColorOrTexture(XmlNode node)
        {
            // only one child !
            XmlNode child = node.FirstChild;
            switch (child.Name)
            {
                case "color":
                    return new Color(this, child);
                case "param":
                    return new ParamRef(this, child);
                case "texture":
                    return new Texture(this, child);
                default:
                    throw new ColladaException("un-expected node <" + child.Name + " in color_or_texture_type :" + filename);
            }
        }
        public IFloatOrParam FloatOrParam(XmlNode node)
        {
            // only one child !
            XmlNode child = node.FirstChild;
            switch (child.Name)
            {
                case "float":
                    return new Float(this, child);
                case "param":
                    return new ParamRef(this, child);
                default:
                    throw new ColladaException("un-expected node <" + child.Name + " in float_or_param_type :" + filename);
                    
            }
        }
        public ITransparent TransparentParam(XmlNode node)
        {
            // only one child !
            XmlNode child = node.FirstChild;
            switch (child.Name)
            {
                case "color":
                    return new TransparentColor(this, child);
                case "param":
                    return new TransparentParamRef(this, child);
                case "texture":
                    return new TransparentTexture(this, child);
                default:
                    throw new ColladaException("un-expected node <" + child.Name + " in transparent parameters :" + filename);
            }
        }

        public T Get<T>(XmlNode node, string param, T defaultValue)
        {
            if (node.Attributes == null) return defaultValue;
            XmlAttribute attrib = node.Attributes[param];
            if (attrib != null) return (T)System.Convert.ChangeType(attrib.Value, typeof(T), encoding);
            else return defaultValue;
        }
        public T[] GetArray<T>(XmlNode node)
        {
            string[] stringValues = Regex.Split(node.InnerText, "[\\s]+");
            int i = 0;
            int k = stringValues.Length;
            if (stringValues[k - 1] == "") k -= 1;
            if (stringValues[0] == "") { i = 1; k--; }
            T[] p = new T[k];

            for (int j = 0; j < k; j++, i++)
                p[j] = (T)System.Convert.ChangeType(stringValues[i], typeof(T), encoding);
            return p;
        }
        
        [Serializable()]
        /// <summary>
        /// Represents the COLLADA "<asset>" element.
        /// </summary>
        public class Asset
        {
            public class Contributor
            {
                public string author;
                public string authoring_tool;
                public string comments;
                public string copyright;
                public string source_data;
                public Contributor() { }
                public Contributor(Document doc, XmlNode node)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        switch (child.Name)
                        {
                            case "author":
                                author = child.InnerText;
                                break;
                            case "authoring_tool":
                                authoring_tool = child.InnerText;
                                break;
                            case "comments":
                                comments = child.InnerText;
                                break;
                            case "copyright":
                                copyright = child.InnerText;
                                break;
                            case "source_data":
                                source_data = child.InnerText;
                                break;
                            default:
                                throw new ColladaException("un-expected <" + child.Name + "> in <asset><contributor> :" + doc.filename);
                        }
                    }

                }
            } // end class Contributor

            public List<Contributor> contributors;
            // TODO: this is a date
            public string created;
            public string keywords;
            // TODO: this is a date
            public string modified;
            public string revision;
            public string subject;
            public string title;
            public string unit = "meter";
            public float meter = 1.0f;
            public string up_axis = "Y_UP";

            private Asset() { }
            public Asset(Document doc, XmlNode node)
            {
                
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "contributor":
                            if (contributors == null) contributors = new List<Contributor>();
                            contributors.Add(new Contributor(doc, child));
                            break;
                        case "created":
                            created = child.InnerText;
                            break;
                        case "modified":
                            modified = child.InnerText;
                            break;
                        case "keywords":
                            keywords = child.InnerText;
                            break;
                        case "revision":
                            revision = child.InnerText;
                            break;
                        case "subject":
                            subject = child.InnerText;
                            break;
                        case "title":
                            title = child.InnerText;
                            break;
                        case "unit":
                            meter = doc.Get<float>(child, "meter", 1.0f);
                            unit = doc.Get<string>(child, "name", null);
                            break;
                        case "up_axis":
                            up_axis = child.InnerText;
                            break;
                        default:
                            throw new ColladaException("un-expected node <" + child.Name + "> in asset :" + doc.filename);
                    }
                }
            }

        }
        /// <summary>
        /// This is a base class shared by a lot of elements.
        /// It contains the id, name and asset information that is contained by many COLLADA elements
        /// </summary>
        [Serializable()]
        public class Element
        {
            public static Random rand = new Random();
            public readonly string id;
            public string name;
            public Asset asset; // note: some elements derive from element, but do *not* have an asset tag
            // TODO: Remove asset from this definition, better be in each element
            private Element() { }
            protected Element(Document doc, string _id)
            {
                id = _id;
                // it is ok to replace another element with the same id with this constructor
                doc.dic[id] = this;
            }
            public Element(Document doc, XmlNode node)
            {
                // get id and name
                id = doc.Get<string>(node, "id", "generatedID_" + rand.Next().ToString());
                if (id != null)
                {
                    if (doc.dic.ContainsKey(id)) throw new NonUniqueIDException("<" + node.Name + "> has non unique id : " + doc.filename);
                    doc.dic[id] = this;
                }
                name = doc.Get<string>(node, "name", null);
                XmlNode child = node.SelectSingleNode("child::asset", doc.nsmgr);
                if (child != null)
                    asset = new Asset(doc, child);
                // NOTE: extra not incuded in Element
            }
        }
        /// <summary>
        /// Represents the COLLADA "<extra>" element.
        /// </summary>
        [Serializable()]
        public class Extra : Element
        {
            public string type;
            public string profile;
            public string value;

            public Extra(Document doc, XmlNode node)
                : base(doc, node)
            {
                type = doc.Get<string>(node, "type", null);
                // TODO: find only *immediate* children
                XmlNode child = node.SelectSingleNode("colladans:technique", doc.nsmgr);
                profile = doc.Get<string>(child, "profile", null);
                if (profile == null) throw new ColladaException("profile missing in <extra><technique>" + doc.filename);
                value = child.InnerText;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<xxx_array>" elements, including float_array, int_array, Name_array....
        /// </summary>
        [Serializable()]
        public class Array<T> : Element
        {
            protected int count; // make it read only to avoid user errors
            public T[] arr;

            public int Count { get { return count; } }
            public T this[int i]
            {
                get { return arr[i]; }
                set { arr[i] = value; }
            }
            public Array(Document _doc, string _id, T[] _arr) : base(_doc,_id)
            {
                arr = _arr;
                count = arr.Length;
            }
            public Array(Document doc, XmlNode arrayElement)
                : base(doc, arrayElement)
            {

                if (id == null) throw new ColladaException("Array [" + arrayElement.Name + "has invalid id :" + doc.filename);
                count = doc.Get<int>(arrayElement, "count", 0);

                arr = new T[count];
                string[] stringValues = arrayElement.InnerText.Split(new Char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (count > 0)
                {
                    int remainder;
                    var times = Math.DivRem(stringValues.Length, count, out remainder);
                    if (times > 1 && remainder == 0)
                    {
                        // Wow - a document using spaces inside the keys ...
                        stringValues = Enumerable.Range(0, count)
                            .Select(i => string.Join(" ", stringValues.Skip(i * times).Take(times)))
                            .ToArray();
                    }
                }
                int arrayCount = 0;
                for (int i = 0; i < stringValues.Length && arrayCount < count; i++)
                {
                    if (stringValues[i] != "")
                    {
                        arr[arrayCount++] = (T)System.Convert.ChangeType(stringValues[i], typeof(T), doc.encoding);
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<param>" element.
        /// </summary>
        [Serializable()]
        public class Param
        {
            public string name;
            public string sid;
            public string semantic;
            public string type;
            public string value;
            //public int index;   // calculated when loading the document
            private Param() { }

            public Param(Document doc, XmlNode node)
            {
                name = doc.Get<string>(node, "name", null);
                sid = doc.Get<string>(node, "sid", null);
                semantic = doc.Get<string>(node, "semantic", null);
                type = doc.Get<string>(node, "type", null);
                if (type == null) throw new ColladaException("missing type information on param " + node + " :" + doc.filename);
                value = node.InnerXml;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<anotate>" element.
        /// </summary>
        [Serializable()]
        public class Annotate
        {
            private string name;
            private string type;
            private string value;
            private Annotate() { }
            public Annotate(Document doc, XmlNode node)
            {
                name = doc.Get<string>(node, "name", null);
                if (name == null) throw new ColladaException("missing name information on annotate " + node + " :" + doc.filename);
                XmlNode child = node.FirstChild;
                type = child.Name;
                value = child.InnerXml;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<samppler2D>" element.
        /// </summary>
        [Serializable()]
        public class Sampler2D : Sampler3D
        {
            // no wrapP in sampler2D

            public Sampler2D(Document doc, XmlNode node)
                : base(doc, node)
            {
            }
        }
        /// <summary>
        /// Represents the COLLADA "<samppler1D>" element.
        /// </summary>
        [Serializable()]
        public class Sampler1D : Sampler3D
        {
            // no wrapP, wrapT in sampler2D

            public Sampler1D(Document doc, XmlNode node)
                : base(doc, node)
            {
            }
        }
        public interface IFxBasicTypeCommon { } ;
        /// <summary>
        /// Represents the COLLADA "<samppler3D>" element.
        /// </summary>
        [Serializable()]
        public class Sampler3D : IFxBasicTypeCommon
        {
            public string source = null;
            public string wrapS = "WRAP";
            public string wrapT = "WRAP";
            public string wrapP = "WRAP";
            public string minFilter = "NONE";
            public string magFilter = "NONE";
            public string mipFilter = "NONE";
            public string borderColor = null;
            public uint mipmapMaxlevel = 255;
            public float mipmapBias = 0.0f;
            List<Extra> extras;

            private Sampler3D() {}

            public Sampler3D(Document doc, XmlNode node)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "source":
                            source = child.InnerText;
                            break;
                        case "wrap_s":
                            wrapS = child.InnerText;
                            break;
                        case "wrap_t":
                            wrapT = child.InnerText;
                            break;
                        case "wrap_p":
                            wrapP = child.InnerText;
                            break;
                        case "minfilter":
                            minFilter = child.InnerText;
                            break;
                        case "magfilter":
                            magFilter = child.InnerText;
                            break;
                        case "mipfilter":
                            mipFilter = child.InnerText;
                            break;
                        case "border_color":
                            borderColor = child.InnerText;
                            break;
                        case "mipmap_maxlevel":
                            mipmapMaxlevel = uint.Parse(child.InnerText, doc.encoding);
                            break;
                        case "mipmap_bias":
                            mipmapBias = float.Parse(child.InnerText, doc.encoding);
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, child));
                            break;

                        default:
                            throw new ColladaException(child.Name + " is not supported in " + this.ToString());
                    }
                }
                
                
            }
        }
        /// <summary>
        /// Represents the COLLADA "<surface>" element.
        /// </summary>
        [Serializable()]
        public class Surface : IFxBasicTypeCommon
        {
            public string type;
            public string initFrom;
            public string format;
            private Surface() { }
            public Surface(Document doc, XmlNode node)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "type":
                            type = child.InnerText;
                            break;
                        case "init_from":
                            initFrom = child.InnerText;
                            break;
                        case "format":
                            format = child.InnerText;
                            break;
                        default:
                            throw new ColladaException(child.Name + " is not recognized in <surface>");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<float[1-4]>" element.
        /// </summary>
        [Serializable()]
        public class FloatVector : IFxBasicTypeCommon
        {
            public float[] values;
            private FloatVector() { }
            public FloatVector(Document doc, XmlNode node)
            {
                string[] stringValues = node.InnerText.Split(new Char[] { ' ' });
                values = new float[stringValues.Length];
                for (int i = 0; i < stringValues.Length; i++)
                    values[i] = float.Parse(stringValues[i], doc.encoding);
            }
        }
        /// <summary>
        /// Represents the COLLADA "<new_param>" element.
        /// </summary>
        [Serializable()]
        public class NewParam
        {
            public string sid;
            private List<Annotate> annotates;
            public string semantic;
            // modifier enum...
            public string modifier;
            public IFxBasicTypeCommon param;

            private NewParam() { }
            public NewParam(Document doc, XmlNode node)
            {
                sid = doc.Get<string>(node, "sid", null);
                if (sid == null) throw new ColladaException("sid is required in newparam" + node + " :" + doc.filename);


                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "semantic":
                            semantic = child.InnerXml;
                            break;
                        case "modifier":
                            modifier = child.InnerXml;
                            break;
                        case "annotate":
                            if (annotates == null) annotates = new List<Annotate>();
                            annotates.Add(new Annotate(doc, child));
                            break;
                        case "surface":
                            param = new Surface(doc,child);
                            break;
                        case "sampler1D":
                            param = new Sampler1D(doc, child);
                            break;
                        case "sampler2D":
                            param = new Sampler2D(doc, child);
                            break;
                        case "sampler3D":
                            param = new Sampler3D(doc, child);
                            break;
                        case "float":
                            param = new FloatVector(doc, child);
                            break;
                        case "float2":
                            param = new FloatVector(doc, child);
                            break;
                        case "float3":
                            param = new FloatVector(doc, child);
                            break;
                        case "float4":
                            param = new FloatVector(doc, child);
                            break;
                        default:
                            throw new ColladaException(child.Name + " is not supported yet in NewParam");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<accessor>" element.
        /// </summary>
        [Serializable()]
        public class Accessor
        {
            public int count = -1;
            public int offset = -1;
            public Locator source;
            public int stride = -1;
            public List<Param> parameters;
            private List<int> paramIndex;
            private Accessor() { }
            public Accessor(Document doc, int _count, int _offset, int _stride, string _source, List<Param> _parameters)
            {
                count = _count;
                offset = _offset;
                stride = _stride;
                source = new Locator(doc,_source);
                parameters = _parameters;
                
                calculateParameterIndex();
            }
            public Accessor(Document doc, XmlNode node)
            {
                count = doc.Get<int>(node, "count", -1);
                if (count < 0) throw new ColladaException("invalid count in accessor");
                offset = doc.Get<int>(node, "offset", 0);
                source = new Locator(doc, node);

                stride = doc.Get<int>(node, "stride", 1);

                foreach (XmlNode paramElement in node.ChildNodes)
                {
                    if (paramElement.Name != "param") throw new ColladaException("Invalid element <" + paramElement.Name + "> in <acessor> :" + doc.filename);
                    if (parameters == null) parameters = new List<Param>();
                    parameters.Add(new Param(doc, paramElement));
                }

                calculateParameterIndex();
            }
            private void calculateParameterIndex()
            {

                paramIndex = new List<int>();
                int index = 0;

                foreach (Param param in parameters)
                {
                    if (param.name != null || param.type != null) // ignore un-named parameters
                    {
                        switch (param.type.ToLower())
                        {
                            case "float4x4":
                                // consume next 16 indices
                                for (int i = 0; i < 16; i++)
                                    paramIndex.Add(index++);
                                break;
                            default:
                                paramIndex.Add(index);
                                break;
                        }
                    }
                    index++;
                }
            }
            // ignore parameters without a name
            public int this[int i] { get { return paramIndex[i]; } }
            public int this[int i, int j] { get { return offset + stride * j + paramIndex[i]; } }
            public int ParameterCount { get {return paramIndex.Count; }}
        }
        public interface ISourceOrVertices
        {
        }
        /// <summary>
        /// Represents the COLLADA "<source>" element.
        /// </summary>
        [Serializable()]
        public class Source : Element, ISourceOrVertices
        {

            public object array;
            public string arrayType;
            public Accessor accessor;
            public Accessor accessorCommon;
            public List<Accessor> accessors;
            public Source(Document doc, string _id, object _array, Accessor _accessorCommon)
                : base(doc, _id)
            {
                accessorCommon = _accessorCommon;
                accessors = new List<Accessor>();
                array = _array;
                
                // fix
                accessor = accessorCommon;
            }
            
            public Source(Document doc, string _id, object _array, Accessor _accessorCommon, List<Accessor> _accessors)
                : base(doc, _id)
            {
                accessorCommon = _accessorCommon;
                accessors = _accessors;
                array = _array;
                
                // fix
                accessor = accessorCommon;
            }
            public Source(Document doc, XmlNode node)
                : base(doc, node)
            {

                if (id == null) throw new ColladaException("Source[" + id + "] does not have id ! : " + doc.filename);
                // Read a source
                // TODO - test if it has a unique array
                accessors = new List<Accessor>();
                
                XmlNode accessorElement;
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "float_array":
                            array = new Array<float>(doc, child);
                            arrayType = child.Name;
                            break;
                        case "int_array":
                            array = new Array<int>(doc, child);
                            arrayType = child.Name;
                            break;
                        case "bool_array":
                            array = new Array<bool>(doc, child);
                            arrayType = child.Name;
                            break;
                        case "IDREF_array":
                            array = new Array<string>(doc, child);
                            arrayType = child.Name;
                            break;
                        case "Name_array":
                            array = new Array<string>(doc, child);
                            arrayType = child.Name;
                            break;
                        case "technique":
                            accessorElement = child.FirstChild;
                            //if (accessorElement == null || accessorElement.Name != "accessor") throw new ColladaException("expected <accessor> in <technique> in <mesh><source>");
                            if (accessorElement == null || accessorElement.Name != "accessor") break; //TODO: handle this case
                            accessors.Add(new Accessor(doc, accessorElement));
                            break;
                        case "technique_common":
                            accessorElement = child.FirstChild;
                            if (accessorElement == null || accessorElement.Name != "accessor") throw new ColladaException("expected <accessor> in <technique_common> in <mesh><source>");
                            accessorCommon = new Accessor(doc, accessorElement);
                            accessor = accessorCommon;
                            break;
                        default:
                            throw new ColladaException("Un recognized array : " + child.Name);
                    }
                }

            }
        }
        
        /// <summary>
        /// Represents the COLLADA "<input>" element.
        /// </summary>
        [Serializable()]
        public class Input
        {
            public int offset;
            public string semantic;
            public ISourceOrVertices source;
            public int set;
            public Document doc;
            private Input() {}
            public Input(Document doc, XmlNode node)
            {
                semantic = doc.Get<string>(node, "semantic", "");
                if (semantic == "") throw new ColladaException("input has no semantic");
                offset = doc.Get<int>(node, "offset", -1);
                set = doc.Get<int>(node, "set", -1);  // need to keep this a int if want to use negative values for special meaning
                Locator loc = new Locator(doc,node);
                source = (ISourceOrVertices)doc.dic[loc.Fragment];
                this.doc = doc;
            }
            public Input(Document doc, int _offset, string _semantic, int _set, string _source)
            {
                offset = _offset;
                semantic = _semantic;
                set = _set;
                source = (ISourceOrVertices)doc.dic[_source];
                if (source == null) throw new ColladaException("Invalid source ");
                this.doc = doc;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<vertices>" element.
        /// </summary>
        [Serializable()]
        public class Vertices : Element, ISourceOrVertices
        {
            public List<Extra> extras;
            public List<Input> inputs;

            //private vertices() { }
            public Vertices(Document doc, string _id, List<Input> _inputs)
                : base(doc, _id)
            {
                inputs = _inputs;
            }
            public Vertices(Document doc, XmlNode node)
                : base(doc, node)
            {

                if (id == null) throw new ColladaException("Vertices[" + id + "] does not have id ! : " + doc.filename);
                
                // Read inputs
                XmlNodeList inputElements = node.SelectNodes("colladans:input", doc.nsmgr);
                if (inputElements.Count != 0) inputs = new List<Input>();
                foreach (XmlNode inputElement in inputElements)
                {
                    inputs.Add(new Input(doc, inputElement));
                }
                // Get Extras
                XmlNodeList extraElements = node.SelectNodes("colladans:extra", doc.nsmgr);
                if (extraElements.Count != 0) extras = new List<Extra>();
                foreach (XmlNode extraElement in extraElements)
                {
                    extras.Add(new Extra(doc, extraElement));
                }
            }
        }
        /// <summary>
        /// Locator is used to store all the URI values one can find in a COLLADA document
        /// </summary>
        [Serializable()]
        public class Locator
        {
            private bool isFragment = false;
            private bool isRelative = false;
            private bool isInvalid = true;
            private Uri url;
            private Locator() { }
            public Locator(Document doc, XmlNode node)
            {
                string path = null;
                if (node.Name == "init_from" || node.Name == "skeleton")
                {
                    path = node.InnerXml;
                }
                else if (node.Name == "instance_material" || node.Name == "bind")
                {
                    path = doc.Get<string>(node, "target", null);
                }
                else if (node.Name == "input" || node.Name == "accessor" || node.Name == "skin" || node.Name == "morph" || node.Name == "channel")
                {
                    path = doc.Get<string>(node, "source", null);
                }
                else
                {
                    path = doc.Get<string>(node, "url", null);
                }
                createLocator(doc,path);
            }
            public Locator(Document doc, string path)
            {
                createLocator(doc,path);
            }
            private void createLocator(Document doc,string path)
            {
                if (path == null || path == "") return;

                if (path.StartsWith("#")) // fragment URI
                {
                    //string relative_path = documentName + path;
                    url = new Uri(doc.baseURI, path);
                    isFragment = true;
                }
                else if (path.Contains(":")) // full uri
                {
                    url = new Uri(path);
                }
                else // relative URI or erronous URI
                {
                    url = new Uri(doc.baseURI, path);
                    isRelative = true;
                }
                isInvalid = false;
                //Console.WriteLine("Found uri = " + url);
            }
            public bool IsFragment { get { return isFragment; } }
            public bool IsRelative { get { return isRelative; } }
            public bool IsInvalid { get { return isInvalid; } }
            public string Fragment
            {
                get
                {
                    if (!isFragment)
                        throw new ColladaException("cannot get Fragment of a non Fragment URI" + this.ToString());
                    // There're documents with stuff like url="#_01 - Default-fx"
                    var fragment = Uri.UnescapeDataString(Uri.Fragment);
                    return fragment.Substring(1);
                    
                }
            }
            public Uri Uri
            {
                get { return url; }
            }
        }
        public interface IColorOrTexture
        {
            // common_color_or_texture_type
            // float or param or texture
        }
        public interface IFloatOrParam
        {
            // common_float_or_param_type
            // float or param
        }
        public interface ITransparent
        {
            // common_color_or_texture_type - float or param or texture
            // + opaque - string(Default A_ONE)
        }
        /// <summary>
        /// Represents the COLLADA "<color>" element.
        /// </summary>
        [Serializable()]
        public class Color : IColorOrTexture
        {
            public string sid;
            public float[] floats;
            private Color() { }
            public Color(Document doc, XmlNode node)
            {
                sid = doc.Get<string>(node, "sid", null);
                floats = doc.GetArray<float>(node);
            }
            public float this[int i]
            {
                get { return floats[i]; }
            }
        }
        [Serializable()]
        public class TransparentFloat : Float, ITransparent
        {
            public string opaque;
            public TransparentFloat(Document doc, XmlNode node)
                : base(doc, node)
            {
                opaque = doc.Get<string>(node, "opaque", "A_ONE");
            }
        }
        [Serializable()]
        public class TransparentTexture : Texture, ITransparent
        {
            public string opaque;
            public TransparentTexture(Document doc, XmlNode node)
                : base(doc, node)
            {
                opaque = doc.Get<string>(node, "opaque", "A_ONE");
            }
        }
        [Serializable()]
        public class TransparentParamRef : ParamRef, ITransparent
        {
            public string opaque;
            public TransparentParamRef(Document doc, XmlNode node)
                : base(doc, node)
            {
                opaque = doc.Get<string>(node, "opaque", "A_ONE");
            }
        }
        [Serializable()]
        public class TransparentColor : Color, ITransparent
        {
            public string opaque;
            public TransparentColor(Document doc, XmlNode node) : base(doc , node)
            {
                opaque = doc.Get<string>(node, "opaque", "A_ONE");
            }
        }
        [Serializable()]
        public class ParamRef : IColorOrTexture, IFloatOrParam
        {
            public string reference;
            private ParamRef() { }
            public ParamRef(Document doc, XmlNode node)
            {
                reference = doc.Get<string>(node, "ref", null);
                if (reference == null) throw new ColladaException("missing mandatory ref parameter in <profile_COMMON><technique><param> :" + doc.filename);
            }
        }
        [Serializable()]
        public class Texture : IColorOrTexture
        {
            public string texture;
            public string texcoord;
            public List<Extra> extras;
            private Texture() { }
            public Texture(Document doc, XmlNode node)
            {
                texture = doc.Get<string>(node, "texture", null);
                if (texture == null) throw new ColladaException("missing texture parameter in <profile_COMMON><technique><texture> :" + doc.filename);
                texcoord = doc.Get<string>(node, "texcoord", null);
                if (texcoord == null)
                {
                    //TODO: strong warning
                    //if (texcoord == null) throw new ColladaException("missing texcoord parameter in <profile_COMMON><technique><texture> :" + doc.filename);
                    texcoord = "MissingTexcoord";
                }
                XmlNodeList extraElements = node.SelectNodes("colladans:extra", doc.nsmgr);
                if (extraElements.Count != 0) extras = new List<Extra>();
                foreach (XmlNode extraElement in extraElements) extras.Add(new Extra(doc, extraElement));
            }

        }

        [Serializable()]
        public class Float : IFloatOrParam
        {
            public string sid;
            public float theFloat;
            private Float() { }
            public Float(string sid, float theFloat) {
            	this.sid = sid;
            	this.theFloat = theFloat;
            }
            public Float(Document doc, XmlNode node)
            {
                sid = doc.Get<string>(node, "sid", null);
                theFloat = float.Parse(node.InnerText, doc.encoding);
            }
        }
        [Serializable()]
        public class SimpleShader
        {
            public IColorOrTexture emission;
            public IColorOrTexture ambient;
            public IColorOrTexture diffuse;
            public IColorOrTexture specular;
            public IFloatOrParam shininess;
            public IColorOrTexture reflective;
            public IFloatOrParam reflectivity;
            public ITransparent transparent;
            public IFloatOrParam transparency;
            public IFloatOrParam indexOfRefraction;
            private SimpleShader() { }
            public SimpleShader(Document doc, XmlNode node)
            {
                
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "emission":
                            emission = doc.ColorOrTexture(child);
                            break;
                        case "ambient":
                            ambient = doc.ColorOrTexture(child);
                            break;
                        case "diffuse":
                            diffuse = doc.ColorOrTexture(child);
                            break;
                        case "specular":
                            specular = doc.ColorOrTexture(child);
                            break;
                        case "shininess":
                            shininess = doc.FloatOrParam(child);
                            break;
                        case "reflective":
                            reflective = doc.ColorOrTexture(child);
                            break;
                        case "reflectivity":
                            reflectivity = doc.FloatOrParam(child);
                            break;
                        case "transparent":
                            transparent = doc.TransparentParam(child);
                            break;
                        case "transparency":
                            transparency = doc.FloatOrParam(child);
                            break;
                        case "index_of_refraction":
                            indexOfRefraction = doc.FloatOrParam(child);
                            break;
                        default:
                            throw new ColladaException("un expected node <" + child.Name + "> in <technique_COMMON><technique> :" + doc.filename);
                    }
                }
            }
        }
        /// <summary>
        /// Represents a COMMON profile constant shader.
        /// </summary>
        [Serializable()]
        public class Constant : SimpleShader
        {
            public Constant(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents a COMMON profile Lambert shader.
        /// </summary>
        [Serializable()]
        public class Lambert : SimpleShader
        {
            public Lambert(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents a COMMON profile Phong shader.
        /// </summary>
        [Serializable()]
        public class Phong : SimpleShader
        {
            public Phong(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents a COMMON profile Blinn shader.
        /// </summary>
        [Serializable()]
        public class Blinn : SimpleShader
        {
            public Blinn(Document doc, XmlNode node) : base(doc, node) { }
        }

        public interface IProfile { };
        /// <summary>
        /// Represents the COLLADA "<profile_COMMON>" element.
        /// </summary>
        [Serializable()]
        public class ProfileCOMMON : Element, IProfile   //    Note: this is missing the name attribute !
        {

            [Serializable()]
            public class Technique : Element
            {
                public string sid;
                public List<Image> images;
                public Dictionary<string, NewParam> newParams;
                public SimpleShader shader;
                //private technique() {}
                public Technique(Document doc, XmlNode node)
                    : base(doc, node)
                {

                    sid = doc.Get<string>(node, "sid", null);

                    foreach (XmlNode child in node.ChildNodes)
                    {
                        switch (child.Name)
                        {
                            case "image":
                                if (images == null) images = new List<Image>();
                                images.Add(new Image(doc, child));
                                break;
                            case "newparam":
                                NewParam tmpNewParam = new NewParam(doc, child);
                                if (newParams == null) newParams = new Dictionary<string, NewParam>();
                                newParams[tmpNewParam.sid] = tmpNewParam;
                                break;
                            case "asset":
                            case "extra":
                                break;
                            case "constant":
                                shader = new Constant(doc, child);
                                break;
                            case "lambert":
                                shader = new Lambert(doc, child);
                                break;
                            case "phong":
                                shader = new Phong(doc, child);
                                break;
                            case "blinn":
                                shader = new Blinn(doc, child);
                                break;
                            default:
                                throw new ColladaException("<profile_COMMON> <technique> un-expected" + child.Name);
                        }
                    }
                }
            }

            public Technique technique;

            public List<Image> images;
            public Dictionary<string,NewParam> newParams;
            public ProfileCOMMON(Document doc, XmlNode node)
                : base(doc, node)
            {

                images = new List<Image>();
                newParams = new Dictionary<string,NewParam>();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "image":
                            images.Add(new Image(doc, child));
                            break;
                        case "technique":
                            technique = new Technique(doc, child);
                            break;
                        case "newparam":
                            NewParam tmpNewParam = new NewParam(doc, child);
                            newParams[tmpNewParam.sid] = tmpNewParam;
                            break;
                        case "asset":
                        case "extra":
                            break;
                        default:
                            throw new ColladaException("un-expected <" + child.Name + "> in profile_COMMON :" + doc.filename);
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<effect>" element.
        /// </summary>
        [Serializable()]
        public class Effect : Element
        {
            public List<Annotate> annotates;
            public List<Image> images;
            public Dictionary<string,NewParam> newparams;
            public List<IProfile> profiles;

            public Locator instance_effect;

            public Effect(Document doc, XmlNode node)
                : base(doc, node)
            {
                if (id == null) throw new ColladaException("Effect[" + id + "] does not have id ! : " + doc.filename);

                // TODO: there can be many profiles !, even common profiles !
                profiles = new List<IProfile>();
                XmlNode profileElement = node.SelectSingleNode("colladans:profile_COMMON", doc.nsmgr);
                if (profileElement == null) throw new ColladaException("effect id=" + id + " has no profile_COMMON :" + doc.filename);
                profiles.Add(new ProfileCOMMON(doc, profileElement));

                // get all images
                XmlNodeList imageElements = node.SelectNodes("colladans:image", doc.nsmgr);
                images = new List<Image>();
                foreach (XmlNode imageElement in imageElements)
                {
                    images.Add(new Image(doc, imageElement));
                }
                // get all newparams
                XmlNodeList newparamElements = node.SelectNodes("colladans:newparam", doc.nsmgr);
                newparams = new Dictionary<string,NewParam>();
                foreach (XmlNode newParamElement in newparamElements)
                {
                    NewParam tmpNewParam = new NewParam(doc, newParamElement);
                    newparams[tmpNewParam.sid] = tmpNewParam;
                }
                // get all annotate
                XmlNodeList annotateElements = node.SelectNodes("colladans:annotate", doc.nsmgr);
                annotates = new List<Annotate>();
                foreach (XmlNode annotateElement in annotateElements)
                {
                    annotates.Add(new Annotate(doc, annotateElement));
                }

            }
        }
        /// <summary>
        /// Represents the COLLADA "<material>" element.
        /// </summary>
        [Serializable()]
        public class Material : Element
        {
            public Locator instanceEffect;

            public Material(Document doc, XmlNode node)
                : base(doc, node)
            {
                if (id == null) throw new ColladaException("Material[" + id + "] does not have id ! : " + doc.filename);
                
                XmlNode instance_effectElement = node.SelectSingleNode("colladans:instance_effect", doc.nsmgr);
                if (instance_effectElement == null) throw new ColladaException("Material[" + id + "] does not have <instance_effect> : " + doc.filename);
                instanceEffect = new Locator(doc, instance_effectElement);
                if (instanceEffect.IsInvalid) throw new ColladaException("Material[" + id + "] does not have url in <instance_effect> : " + doc.filename);
            }
        }
        /// <summary>
        /// base class used to represent all the COLLADA primitives (triangles, lines, polygons...
        /// </summary>
        [Serializable()]
        public class Primitive
        {
            public string name;
            public string material;
            public int count;
            protected List<Input> inputs;
            public int stride = 0; // maximum offset + 1
            public int[] p;
            public int[] vcount;
            public List<Extra> extras;

            public List<Input> Inputs
            {
                get { return inputs; }
                set
                {
                    inputs = value;
                    stride = 0; // maximum offset + 1
                    foreach (Input input in inputs)
                        if (input.offset >= stride) stride = input.offset + 1;
                }
            }
            private Primitive () {}
            protected Primitive(Document doc, List<Input> _inputs, int[] _p)
            {
                p = _p;
                Inputs = _inputs;
            }
            public Primitive(Document doc, XmlNode node)
            {
                name = doc.Get<string>(node, "name", "");
                count = doc.Get<int>(node, "count", -1);
                if (count <= 0) throw new ColladaException("count <=0 in <triangle:");
                material = doc.Get<string>(node, "material", "");

                // Read <input> <p> and <extra>
                inputs = new List<Input> ();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "input":
                            Input input = new Input(doc, child);
                            inputs.Add(input);
                            if (input.offset >= stride) stride = input.offset + 1;
                            break;
                        case "p":
                            p = doc.GetArray<int>(child);
                            break;
                        case "vcount":
                            vcount = doc.GetArray<int>(child);
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, child));
                            break;
                        default:
                            throw new ColladaException("un-recognized element " + child.Name + " in triangle");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<triangle>" element.
        /// </summary>
        [Serializable()]
        public class Triangle : Primitive
        {
            public Triangle(Document doc, XmlNode node) : base(doc,node) {}
            public Triangle(Document doc,  int _count, List<Input> _inputs, int[] _p)
                : base(doc, _inputs, _p)
            {
                count = _count;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<line>" element.
        /// </summary>
        [Serializable()]
        public class Line : Primitive
        {
            public Line(Document doc, XmlNode node) : base(doc, node) { }
            public Line(Document doc,  int _count, List<Input> _inputs, int[] _p)
                : base(doc, _inputs, _p)
            {
                count = _count;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<polylist>" element.
        /// </summary>
        [Serializable()]
        public class Polylist : Primitive
        {
            public Polylist(Document doc, XmlNode node) : base(doc, node) { }
            public Polylist(Document doc, int _count, List<Input> _inputs, int[] _p)
                : base(doc, _inputs, _p)
            {
                count = _count;
            }
        }
        /// <summary>
        /// Represents the COLLADA "<mesh>" element.
        /// </summary>
        [Serializable()]
        public class Mesh
        {
            //get all the sources
            public List<Source> sources;
            public List<Primitive> primitives;
            public Vertices vertices;

            private Mesh() { }
            public Mesh(Document doc, XmlNode node)
            {
                primitives = new List<Primitive>();
                sources = new List<Source>();
                
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "source":
                            sources.Add(new Source(doc, child));
                            break;
                        case "vertices":
                            vertices = new Vertices(doc, child);
                            break;
                        case "triangles":
                            primitives.Add(new Triangle(doc, child));
                            break;
                        case "polylist":
                            primitives.Add(new Polylist(doc, child));
                            break;
                        case "lines":
                            primitives.Add(new Line(doc, child));
                            break;
                        default:
                            throw new ColladaException(child.Name+" type not supported yet");
                    }
                }

                if (sources.Count == 0) throw new ColladaException("<mesh does not contain a <source> : " + doc.filename);
                if (vertices == null) throw new ColladaException("<mesh> does not contain a <vertices> : " + doc.filename);
                
            }
            
        }
        /// <summary>
        /// Base class to represent all the possible transforms in COLLADA.
        /// </summary>
        [Serializable()]
        public class TransformNode
        {
            public static Random rand = new Random();
            protected float[] floats;
            public string sid;
            private TransformNode() { }
            public TransformNode(Document doc, XmlNode node)
            {
                sid = doc.Get<string>(node, "sid", "generatedSID_transformNode_" + rand.Next().ToString());
                floats = doc.GetArray<float>(node);
            }
            public float this[int i]
            {
                get { return floats[i]; }
                set { floats[i] = value; }
            }
            public int Size { get { return floats.Length; } }
        }
        /// <summary>
        /// Represents the COLLADA "<lookat>" element.
        /// </summary>
        [Serializable()]
        public class Lookat : TransformNode
        {
            public Lookat(Document doc, XmlNode node) : base(doc, node) { }
            public float this[int i, int j]
            {
                get { return floats[3 * i + j]; }
                set { floats[3 * i + j] = value; }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<lmatrix>" element.
        /// </summary>
        [Serializable()]
        public class Matrix : TransformNode
        {
            public Matrix(Document doc, XmlNode node) : base(doc, node) { }
            public float this[int i, int j]
            {
                get { return floats[4 * i + j]; }
                set { floats[4 * i + j] = value; }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<rotate>" element.
        /// </summary>
        [Serializable()]
        public class Rotate : TransformNode
        {
            public Rotate(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents the COLLADA "<scale>" element.
        /// </summary>
        [Serializable()]
        public class Scale : TransformNode
        {
            public Scale(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents the COLLADA "<translate>" element.
        /// </summary>
        [Serializable()]
        public class Translate : TransformNode
        {
            public Translate(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Represents the COLLADA "<skew>" element.
        /// </summary>
        [Serializable()]
        public class Skew : TransformNode
        {
            public Skew(Document doc, XmlNode node) : base(doc, node) { }
        }
        /// <summary>
        /// Base class to represent COLLADA instances.
        /// </summary>
        [Serializable()]
        public class Instance
        {
            List<Extra> extras;
            public Locator url;
            public string sid;
            public string name;
            private Instance() { }
            public Instance(Document doc, XmlNode node)
            {
                url = new Locator(doc, node);
                if (url.IsInvalid) throw new ColladaException("missing url in " + node.Name + " in file:" + doc.filename);
                sid = doc.Get<string>(node, "sid", null);
                name = doc.Get<string>(node, "name", null);
                XmlNodeList extraElements = node.SelectNodes("colladans:extra", doc.nsmgr);
                if (extraElements.Count != 0) extras = new List<Extra>();
                foreach (XmlNode extraElement in extraElements) extras.Add(new Extra(doc, extraElement));
            }
        }

        [Serializable()]
        public class InstanceWithMaterialBind : Instance
        {
            public BindMaterial bindMaterial;
            public InstanceWithMaterialBind(Document doc, XmlNode node)
                : base(doc, node)
            {
                XmlNode bindMaterialElement = node.SelectSingleNode("colladans:bind_material", doc.nsmgr);
                if (bindMaterialElement != null)
                    bindMaterial = new BindMaterial(doc, bindMaterialElement);
            }
        }
        /// <summary>
        /// Represents the COLLADA "<instance_camera>" element.
        /// </summary>
        [Serializable()]
        public class InstanceCamera : Instance
        {
            public InstanceCamera(Document doc, XmlNode node) : base(doc, node) { }  // constructor
        };
        /// <summary>
        /// Represents the COLLADA "<instance_controller>" element.
        /// </summary>
        [Serializable()]
        public class InstanceController : InstanceWithMaterialBind
        {
            private Document doc;
            private List<Locator> skeleton;
            public List<Locator> Skeleton
            {
                get
                {
                    if (skeleton == null)
                    {
                        // No skeleton given. A behaviour seen in 3dmax.
                        // Try to find a skeleton...
                        
                        Node rootJoint = FindFirstRootJoint(doc);
                        if (rootJoint != null)
                        {
                            skeleton = new List<Locator>();
                            skeleton.Add(new Locator(doc, "#" + rootJoint.id));
                        }
                    }
                    return skeleton;
                }
            }

            public InstanceController(Document doc, XmlNode node)
                : base(doc, node)
            {
                this.doc = doc;

                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "skeleton":
                            if (skeleton == null) skeleton = new List<Locator>();
                            skeleton.Add(new Locator(doc, child));
                            break;
                        case "bind_material":
                            // base class takes care of bind_material
                            break;
                        case "extra":
                            // Instance base class takes care of extra already
                            break;
                        default:
                            throw new ColladaException("unexpected <" + child.Name + "> in <instance_controller> :" + doc.filename);

                    }
                }
            }
            
            protected Node FindFirstRootJoint(Document doc)
            {
                foreach (VisualScene vs in doc.visualScenes)
                {
                    var result = FindFirstRootJoint(vs.nodes);
                    if (result != null)
                    {
                        return result;
                    }
                }
                
                return null;
            }
            
            protected Node FindFirstRootJoint(List<Node> nodes)
            {
                var result = nodes.FirstOrDefault(n => n.type.ToLower() == "joint");
                if (result != null)
                {
                    return result;
                }
                else
                {
                    foreach (var n in nodes)
                    {
                        if (n.children != null)
                        {
                            result = FindFirstRootJoint(n.children);
                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                }
                
                return null;
            }
        };
        /// <summary>
        /// Represents the COLLADA "<instance_material>" element.
        /// </summary>
        [Serializable()]
        public class InstanceMaterial
        {
            public string symbol;
            public Locator target;
            public string sid;
            public string name;
            [Serializable()]
            public struct Bind
            {
                public string semantic;
                public Locator target;
            };
            public List<Bind> binds;
            [Serializable()]
            public struct BindVertexInput
            {
                public string semantic;
                public string inputSemantic;
                public uint inputSet;
            };
            public List<BindVertexInput> bindVertexInputs;

            public List<Extra> extras;
            private InstanceMaterial() { }
            public InstanceMaterial(Document doc, XmlNode node)
            {
                symbol = doc.Get<string>(node, "symbol", null);
                if (symbol == null) throw new ColladaException("missing symbol parameter in instance_material" + node + " :" + doc.filename);
                target = new Locator(doc, node);
                if (target == null) throw new ColladaException("missing target parameter in instance_material" + node + " :" + doc.filename);
                sid = doc.Get<string>(node, "sid", null);
                name = doc.Get<string>(node, "sid", null);

                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "bind":
                            {
                                Bind tmp;
                                tmp.semantic = doc.Get<string>(child, "semantic", null);
                                if (tmp.semantic == null) throw new ColladaException("invalid semantic in <instance_material><bind> :" + doc.filename);
                                tmp.target = new Locator(doc, node);
                                if (tmp.target == null) throw new ColladaException("invalid target in <instance_material><bind> :" + doc.filename);
                                if (binds == null) binds = new List<Bind>();
                                binds.Add(tmp);
                            }
                            break;
                        case "bind_vertex_input":
                            {
                                BindVertexInput tmp;
                                tmp.semantic = doc.Get<string>(child, "semantic", null);
                                if (tmp.semantic == null) throw new ColladaException("invalid semantic in <instance_material><bind> :" + doc.filename);
                                tmp.inputSemantic = doc.Get<string>(child, "input_semantic", null);
                                if (tmp.inputSemantic == null) throw new ColladaException("invalid input_semantic in <instance_material><bind> :" + doc.filename);
                                tmp.inputSet = doc.Get<uint>(child, "input_set", 0);
                                if (bindVertexInputs == null) bindVertexInputs = new List<BindVertexInput>();
                                bindVertexInputs.Add(tmp);
                            }
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, child));
                            break;
                        default:
                            throw new ColladaException("un-expected node " + child.Name + " in <instance_material> :" + doc.filename);

                    }
                }
            }

        }
        /// <summary>
        /// Represents the COLLADA "<bind_material>" element.
        /// </summary>
        [Serializable()]
        public class BindMaterial : Element
        {
            public Dictionary<string, Param> parameters;
            public Dictionary<string,InstanceMaterial> instanceMaterials;
            //public List<technique
            public List<Extra> extras;

            public BindMaterial(Document doc, XmlNode node)
                : base(doc, node)
            {


                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "param":
                            Param param = new Param(doc, child);
                            if (parameters == null) parameters = new Dictionary<string,Param>();
                            parameters[param.name]=param;
                            break;
                        case "technique_common":
                            foreach (XmlNode temp in child.ChildNodes)
                            {
                                if (temp.Name != "instance_material") throw new ColladaException("illegal node <" + temp.Name + "> in <bind_material><technique_common> :" + doc.filename);
                                if (instanceMaterials == null) instanceMaterials = new Dictionary<string,InstanceMaterial>();
                                InstanceMaterial tmpInstanceMaterial = new InstanceMaterial(doc, temp);
                                instanceMaterials[tmpInstanceMaterial.target.Fragment] = tmpInstanceMaterial;
                            }
                            break;
                        case "technique":
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, child));
                            break;
                        default:
                            throw new ColladaException("un-expected node " + child.Name + " in <instance_material> :" + doc.filename);

                    }
                }

            }
        }
        /// <summary>
        /// Represents the COLLADA "<instance_geometry>" element.
        /// </summary>
        [Serializable()]
        public class InstanceGeometry : InstanceWithMaterialBind
        {
            public InstanceGeometry(Document doc, XmlNode node)
                : base(doc, node)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "bind_material":
                            // base class takes care of bind_material
                            break;
                        case "extra":
                            // Intance constructor take care of extra already
                            break;
                        default:
                            throw new ColladaException("unexpected <" + child.Name + "> in <instance_controler> :" + doc.filename);

                    }
                }
            } // constructor
        };
        /// <summary>
        /// Represents the COLLADA "<instance_light>" element.
        /// </summary>
        [Serializable()]
        public class InstanceLight : Instance
        {
            public InstanceLight(Document doc, XmlNode node) : base(doc, node) { } // constructor
        } ;
        /// <summary>
        /// Represents the COLLADA "<instance_node>" element.
        /// </summary>
        public class InstanceNode : Instance
        {
            public InstanceNode(Document doc, XmlNode node) : base(doc, node) { } // constructor
        } ;
        /// <summary>
        /// Represents the COLLADA "<node>" element.
        /// </summary>
        [Serializable()]
        public class Node : Element
        {
            public string sid;
            public string type;
            public string layer;
            public List<TransformNode> transforms;
            public List<Node> children;
            public List<Instance> instances;
            public List<Extra> extras;

            public Node(Document doc, XmlNode root)
                : base(doc, root)
            {

                sid = doc.Get<string>(root, "sid", id);
                type = doc.Get<string>(root, "type", "NODE");
                layer = doc.Get<string>(root, "layer", null);

                foreach (XmlNode childElement in root.ChildNodes)
                {
                    switch (childElement.Name)
                    {
                        case "lookat":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Lookat(doc, childElement));
                            break;
                        case "matrix":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Matrix(doc, childElement));
                            break;
                        case "rotate":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Rotate(doc, childElement));
                            break;
                        case "scale":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Scale(doc, childElement));
                            break;
                        case "skew":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Skew(doc, childElement));
                            break;
                        case "translate":
                            if (transforms == null) transforms = new List<TransformNode>();
                            transforms.Add(new Translate(doc, childElement));
                            break;
                        case "instance_camera":
                            if (instances == null) instances = new List<Instance>();
                            instances.Add(new InstanceCamera(doc, childElement));
                            break;
                        case "instance_controller":
                            if (instances == null) instances = new List<Instance>();
                            instances.Add(new InstanceController(doc, childElement));
                            break;
                        case "instance_geometry":
                            if (instances == null) instances = new List<Instance>();
                            instances.Add(new InstanceGeometry(doc, childElement));
                            break;
                        case "instance_light":
                            if (instances == null) instances = new List<Instance>();
                            instances.Add(new InstanceLight(doc, childElement));
                            break;
                        case "instance_node":
                            if (instances == null) instances = new List<Instance>();
                            instances.Add(new InstanceNode(doc, childElement));
                            break;
                        case "node":
                            if (children == null) children = new List<Node>();
                            children.Add(new Node(doc, childElement)); // recursive call
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, childElement));
                            break;
                        default:
                            throw new ColladaException("invalid node[" + childElement.Name + "] in <node> in file " + doc.filename);
                    }
                }

            }
        }
        /// <summary>
        /// Represents the COLLADA "<visual_scene>" element.
        /// </summary>
        [Serializable()]
        public class VisualScene : Element
        {
            public List<Node> nodes;
            //private evaluate_scene List<>
            //private visual_scene() { }
            public VisualScene(Document doc, XmlNode node)
                : base(doc, node)
            {

                XmlNodeList nodeElements = node.SelectNodes("colladans:node", doc.nsmgr);
                if (nodeElements.Count == 0) throw new MissingRequiredElementException("visual_scene[" + id + "] does not contain a <node> : " + doc.filename);
                nodes = new List<Node>();
                foreach (XmlNode nodeElement in nodeElements)
                {
                    nodes.Add(new Node(doc, nodeElement));
                }
            }
        }
        public interface ISkinOrMorph
        {
            //Locator Source;
        }
        [Serializable()]
        public class Skin : ISkinOrMorph
        {
            public Locator source;
            public List<Source> sources;
            [Serializable()]
            public struct Joint
            {
                public List<Input> inputs;
                public List<Extra> extras;
            }
            public Joint joint;
            [Serializable()]
            public struct VertexWeights
            {
                public uint count;
                public List<Input> inputs;
                public uint[] vcount;
                public int[] v;
                public List<Extra> extras;
            }
            public VertexWeights vertexWeights;
            public List<Extra> extras;
            public Matrix bindShapeMatrix;

            public Skin(Document doc, XmlNode node)
            {
                source = new Locator(doc, node);

                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "bind_shape_matrix":
                            bindShapeMatrix = new Matrix(doc,child);
                            break;
                        case "source":
                            if (sources == null) sources = new List<Source>();
                            sources.Add(new Source(doc, child));
                            break;
                        case "joints":
                            // grab all the sub-elements
                            XmlNodeList inputElements = child.SelectNodes("colladans:input", doc.nsmgr);
                            if (inputElements.Count != 0)
                                joint.inputs = new List<Input>();
                            else
                                throw new ColladaException ("no <input> elements in <skin><joints>");
                            foreach (XmlNode inputElement in inputElements)
                            {
                                joint.inputs.Add(new Input(doc,inputElement));
                            }
                            // Do the same for EXTRA !
                            XmlNodeList extraElements = child.SelectNodes("colladans:extra", doc.nsmgr);
                            if (extraElements.Count != 0)
                                joint.extras = new List<Extra>();
                            foreach (XmlNode extraElement in extraElements)
                            {
                                joint.extras.Add(new Extra(doc, extraElement));
                            }
                            break;
                        case "vertex_weights":
                            vertexWeights.count = doc.Get<uint>(child, "count", 0);
                            // grab all the sub-elements
                            inputElements = child.SelectNodes("colladans:input", doc.nsmgr);
                            if (inputElements.Count >= 2)
                                vertexWeights.inputs = new List<Input>();
                            else
                                throw new ColladaException("need at least 2 <input> elements in <skin><vertex_weights>");
                            foreach (XmlNode inputElement in inputElements)
                            {
                                vertexWeights.inputs.Add(new Input(doc, inputElement));
                            }
                            // Do the same for EXTRA !
                            
                            extraElements = child.SelectNodes("colladans:extra", doc.nsmgr);
                            if (extraElements.Count != 0)
                                vertexWeights.extras = new List<Extra>();
                            foreach (XmlNode extraElement in extraElements)
                            {
                                vertexWeights.extras.Add(new Extra(doc, extraElement));
                            }
                            vertexWeights.vcount =  doc.GetArray<uint>(child.SelectSingleNode("colladans:vcount", doc.nsmgr));
                            vertexWeights.v = doc.GetArray<int>(child.SelectSingleNode("colladans:v", doc.nsmgr));
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc,child));
                            break;
                        default:
                            throw new ColladaException("invalide node "+child.Name+ "in <Skin>");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<morph>" element.
        /// </summary>
        [Serializable()]
        public class Morph : ISkinOrMorph
        {
            Locator source;
            List<Source> sources;
            public string method;
            public class Target
            {
                public List<Input> inputs;
                public List<Extra> extras;
            }
            public Target target;
            public List<Extra> extras;
            public Morph(Document doc, XmlNode node)
            {
                source = new Locator(doc, node);
                target = new Target();
                method = doc.Get<string>(node, "method", "NORMALIZED");
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "source":
                            if (sources == null) sources = new List<Source>();
                            sources.Add(new Source(doc, child));
                            break;
                        case "targets":
                            // grab all the sub-elements
                            XmlNodeList inputElements = child.SelectNodes("colladans:input", doc.nsmgr);
                            if (inputElements.Count != 0)
                                target.inputs = new List<Input>();
                            else
                                throw new ColladaException("no <input> elements in <skin><joints>");
                            foreach (XmlNode inputElement in inputElements)
                            {
                                target.inputs.Add(new Input(doc, inputElement));
                            }
                            // Do the same for EXTRA !
                            XmlNodeList extraElements = child.SelectNodes("colladans:extra", doc.nsmgr);
                            if (extraElements.Count != 0)
                                target.extras = new List<Extra>();
                            foreach (XmlNode extraElement in extraElements)
                            {
                                target.extras.Add(new Extra(doc, extraElement));
                            }
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add( new Extra(doc,child));
                            break;
                        default:
                            throw new ColladaException("invalide node " + child.Name + "in <Skin>");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<controller>" element.
        /// </summary>
        [Serializable()]
        public class Controller : Element
        {
            public ISkinOrMorph controller;
            public List<Extra> extras;
            public Controller(Document doc, XmlNode node)
                : base(doc, node)
            {
                if (id == null) throw new ColladaException("Controller does not have id ! : " + doc.filename);

                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "skin":
                            controller = new Skin(doc, child);
                            break;
                        case "morph":
                            controller = new Morph(doc, child);
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc,child));
                            break;
                        default:
                            throw new ColladaException ("Invalid node "+child.Name+" type in <controller>");
                    }
                }

            }
        }
        /// <summary>
        /// Represents the COLLADA "<sampler>" element.
        /// </summary>
        [Serializable()]
        public class Sampler : Element
        {
            public List<Input> inputs;
            public Sampler(Document doc, XmlNode node)
                : base(doc, node)
            {
                if (id == null) throw new ColladaException("Controller does not have id ! : " + doc.filename);

                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "input":
                            if (inputs == null) inputs = new List<Input>();
                            inputs.Add( new Input(doc, child));
                            break;
                        default:
                            throw new ColladaException("Invalid node " + child.Name + " type in <sampler>");
                    }
                }
                
                if (inputs.Count == 0)
                    throw new MissingRequiredElementException("<sampler> must contain at least one <input> element!");

            }
        }
        /// <summary>
        /// Base class to represent COLLADA <channel>.
        /// </summary>
        [Serializable()]
        public class Channel
        {
            public Sampler source;
            public string target;
            private Channel() { }
            public Channel(Document doc, XmlNode node)
            {
                Locator loc = new Locator(doc, node);
                source = (Sampler)doc.dic[loc.Fragment];
                target = doc.Get<string>(node, "target", null);
            }
        }
        /// <summary>
        /// Represents the COLLADA "<animation>" element.
        /// </summary>
        [Serializable()]
        public class Animation : Element // id, name
        {
            public List<Extra> extras;
            public List<Source> sources;
            public List<Sampler> samplers;
            public List<Channel> channels;
            public List<Animation> children;

            public Animation(Document doc, XmlNode node)
                : base(doc, node)
            {

                if (id == null) throw new ColladaException("Animation does not have id ! : " + doc.filename);
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "source":
                            if (sources == null) sources = new List<Source>();
                            sources.Add( new Source(doc, child));
                            break;
                        case "sampler":
                            if (samplers == null) samplers = new List<Sampler>();
                            samplers.Add(new Sampler(doc, child));
                            break;
                        case "channel":
                            if (channels == null) channels = new List<Channel>();
                            channels.Add(new Channel(doc, child));
                            break;
                        case "asset":
                            break;
                        case "extra":
                            if (extras == null) extras = new List<Extra>();
                            extras.Add(new Extra(doc, child));
                            break;
                        case "animation":
                            if (children == null) children = new List<Animation>();
                            children.Add(new Animation(doc, child));
                            break;
                        default:
                            throw new ColladaException("Invalid node '" + child.Name + "' type in <animation>");
                    }
                }
            }
        }
        /// <summary>
        /// Represents the COLLADA "<geometry>" element.
        /// </summary>
        [Serializable()]
        public class Geometry : Element
        {
            public Mesh mesh;

            public Geometry(Document doc, XmlNode node)
                : base(doc, node)
            {

                if (id == null) throw new ColladaException("Geometry[" + id + "] does not have id ! : " + doc.filename);


                // contains only one type of mesh
                XmlNode meshElement = node.SelectSingleNode("colladans:mesh", doc.nsmgr);
                if (meshElement == null) throw new ColladaException("Geometry[" + id + "] does not contain a <mesh> : " + doc.filename);

                // TODO: convex_mesh and spline

                // read mesh
                mesh = new Mesh(doc, meshElement);
            }
        }
        public abstract class PerspectiveOrOrthographic {
        	public Float aspect_ratio;
        	public Float znear;
        	public Float zfar;
        	
        	public PerspectiveOrOrthographic(Document doc, XmlNode node)
            {
        		foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "aspect_ratio":
                            aspect_ratio = new Float(doc, child);
                            break;
                        case "znear":
                            znear = new Float(doc, child);
                            break;
                        case "zfar":
                            zfar = new Float(doc, child);
                            break;
                    }
                }
        		
        		if (znear == null || zfar == null)
        			throw new ColladaException("Missing znear or zfar in <" + node.Name + ">");
        	}
        }
        	
        /// <summary>
        /// Represents the COLLADA "<orthographic>" element.
        /// </summary>
        public class Orthographic : PerspectiveOrOrthographic
        {
        	public Float xmag;
        	public Float ymag;
        	public List<Extra> extras;
        	
        	public Orthographic(Document doc, XmlNode node)
                : base(doc, node)
            {
        		foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "xmag":
                    		xmag = new Float(doc, child);
                            break;
                        case "ymag":
                            ymag = new Float(doc, child);
                            break;
                    }
                }
        		
        		// aspect_ratio = xmag / ymag
        		if (aspect_ratio == null) {
        			if (xmag == null) {
        				if (ymag == null) {
        					throw new ColladaException("Missing xmag, ymag and aspectRatio in <orthographic>");
        				} else {
        		            aspect_ratio = new Float(null, 1);
        					xmag = new Float(null, aspect_ratio.theFloat * ymag.theFloat);
        				}
        			} else {
        				if (ymag == null) {
        		            aspect_ratio = new Float(null, 1);
        					ymag = new Float(null, xmag.theFloat / aspect_ratio.theFloat);
        		        } else {
        				    aspect_ratio = new Float(null, xmag.theFloat / ymag.theFloat);
        		        }
        			}
        		} else {
        			if (xmag == null) {
        				if (ymag == null) {
        					throw new ColladaException("Missing xmag or ymag to aspectRatio in <orthographic>");
        				} else {
        					xmag = new Float(null, aspect_ratio.theFloat * ymag.theFloat);
        				}
        			} else {
        				if (ymag == null) {
        					ymag = new Float(null, xmag.theFloat / aspect_ratio.theFloat);
        				}
        			}
        		}
        	}
        }
        /// <summary>
        /// Represents the COLLADA "<perspective>" element.
        /// </summary>
        public class Perspective : PerspectiveOrOrthographic
        {
        	public Float xfov;
        	public Float yfov;
        	
        	public Perspective(Document doc, XmlNode node)
                : base(doc, node)
            {
        		foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "xfov":
                    		xfov = new Float(doc, child);
                            break;
                        case "yfov":
                            yfov = new Float(doc, child);
                            break;
                    }
                }
        		
        		// aspect_ratio = xfov / yfov
        		if (aspect_ratio == null) 
        		{
        			if (xfov == null) 
        			{
        				if (yfov == null) 
        					throw new ColladaException("Missing xfov, yfov and aspectRatio in <perspective>");
        				else 
        				{
        				    aspect_ratio = new Float(null, 1);
        				    xfov = new Float(null, aspect_ratio.theFloat * yfov.theFloat);
        				}
        			} 
        			else
        			{
        				if (yfov == null)
        				{
        				    aspect_ratio = new Float(null, 1);
        				    yfov = new Float(null, xfov.theFloat / aspect_ratio.theFloat);
        				}
        				else
        				{
        				    aspect_ratio = new Float(null, xfov.theFloat / yfov.theFloat);
        				}
        			}
        		} 
        		else
        		{
        			if (xfov == null) 
        			{
        				if (yfov == null) 
        					throw new ColladaException("Missing xfov or yfov to aspectRatio in <perspective>");
        				else
        					xfov = new Float(null, aspect_ratio.theFloat * yfov.theFloat);
        			} 
        			else 
        			{
        				if (yfov == null) 
        					yfov = new Float(null, xfov.theFloat / aspect_ratio.theFloat);
        			}
        		}
        	}
        }
        /// <summary>
        /// Represents the COLLADA "<optics>" element.
        /// </summary>
        public class Optics
        {
        	public PerspectiveOrOrthographic perspectiveOrOrthographic;
        	public List<Extra> extras;
        	
        	private Optics() { }
        	public Optics(Document doc, XmlNode node) 
        	{
        		foreach (XmlNode child in node.ChildNodes) 
        		{
        			switch(child.Name) 
        			{
    					case "technique_common":
                        foreach (XmlNode temp in child.ChildNodes)
                        {
                        	if (temp.Name == "perspective") {
                        		perspectiveOrOrthographic = new Perspective(doc, temp);
                        	} else if (temp.Name == "orthographic") {
                        		perspectiveOrOrthographic = new Orthographic(doc, temp);
                        	} else
                            	throw new ColladaException("Illegal node <" + temp.Name + "> in <optics><technique_common> :" + doc.filename);
                        }
                        break;
                        
                       	case "extra":
                        if (extras == null) extras = new List<Extra>();
                        extras.Add(new Extra(doc, child));
                        break;
                        
                       	case "technique":
                        break;
        			}
        		}
        		
        		if (perspectiveOrOrthographic == null)
        			throw new ColladaException("Missing technique_common in <optics>");
        	}
        }
        /// <summary>
        /// Represents the COLLADA "<camera>" element.
        /// </summary>
        [Serializable()]
        public class Camera : Element
        {
        	public Optics optics;
        	public List<Extra> extras;
        	
        	public Camera(Document doc, XmlNode node)
                : base(doc, node)
            {
        		foreach (XmlNode child in node.ChildNodes) 
        		{
        			switch(child.Name) 
        			{
        				case "optics":
        					optics = new Optics(doc, child);
        					break;
        				case "imager":
        					break;
        				case "extra":
        					if (extras == null) extras = new List<Extra>();
        					extras.Add(new Extra(doc, child));
        					break;
        			}
        		}
        		
        		if (optics == null)
        			throw new ColladaException("Missing optics in <camera>");
        	}
        }
        /// <summary>
        /// Represents the COLLADA "<image>" element.
        /// </summary>
        [Serializable()]
        public class Image : Element
        {
            public bool isData;
            public Locator init_from;
            public string data;
            public string format;
            public int height;
            public int width;
            public int depth;

            // private image() { }
            public Image(Document doc, XmlNode node)
                : base(doc, node)
            {
                if (id == null) throw new ColladaException("Image[" + id + "] does not have id ! : " + doc.filename);

                format = doc.Get<string>(node, "format", "");
                height = doc.Get<int>(node, "height", -1);
                width = doc.Get<int>(node, "width", -1);
                depth = doc.Get<int>(node, "depth", -1);

                XmlNode dataElement = node.SelectSingleNode("colladans:data", doc.nsmgr);
                XmlNode init_fromElement = node.SelectSingleNode("colladans:init_from", doc.nsmgr);

                if (dataElement != null)
                {
                    isData = true;
                    // TODO: load image from DATA
                    data = dataElement.InnerXml;
                }
                else if (init_fromElement != null)
                {
                    isData = false;
                    init_from = new Locator(doc, init_fromElement);
                    if (init_from.IsInvalid) throw new ColladaException("<image> <init_from> is invalid URL :" + doc.filename);
                }
                else throw new ColladaException("Image[" + id + "] does not contain either <init_from> or <data>: " + doc.filename);
            }
        }
        /// <summary>
        /// Represents the COLLADA "<instance_scene>" element.
        /// </summary>
        [Serializable()]
        public class InstanceScene
        {
            public Locator url;
            public string sid;
            public string name;
            private InstanceScene() { }
            public InstanceScene(Document doc, XmlNode node)
            {
                url = new Locator(doc, node);
                if (url.IsInvalid) throw new ColladaException("instance_scene[" + node.Name + "] does not have url ! : " + doc.filename);
                sid = doc.Get<string>(node, "sid", null);
                name = doc.Get<string>(node, "sid", name);
            }
        }
        // public abstract T Import(string filename, ContentImporterContext context);

        [NonSerialized()]
        private XmlNode root = null;
        public List<Image> images;
        public List<Material> materials;
        public List<Effect> effects;
        public List<Geometry> geometries;
        public List<Controller> controllers;
        public List<Node> nodes;
        public List<VisualScene> visualScenes;
        public List<Animation> animations;
        public List<Camera> cameras;
        public Asset asset;
        public InstanceScene instanceVisualScene;
        public InstanceScene instancePhysicsScene;

        public Document()
        {

            dic = new Hashtable();
            dic.Clear();
            colladaDocument = new XmlDocument();
            nsmgr = new XmlNamespaceManager(colladaDocument.NameTable);
            nsmgr.AddNamespace("colladans", "http://www.collada.org/2005/11/COLLADASchema");
            encoding = new System.Globalization.CultureInfo("en-US");

        }
        /// <summary>
        /// Loads a COLLADA document from a file. Returns a Document object.
        /// <param name="name"> is the name of the file to be loaded </param>
        /// </summary>
        public Document(string name)
            : this()
        {
            filename = name;
            if (!File.Exists(filename))
                throw new FileNotFoundException("Could ot find file:" + filename);
            colladaDocument = new XmlDocument();
            colladaDocument.Load(filename);
            root = colladaDocument.DocumentElement;
            if (root.Name != "COLLADA")
                throw new ColladaException("This file " + filename + "contains a " + root.Name + ", expected COLLADA");
            int split = colladaDocument.BaseURI.LastIndexOf("/");
            baseURI = new Uri(colladaDocument.BaseURI);
            documentName = colladaDocument.BaseURI.Substring(split + 1);
            // get encoding scheme rom xml document, default to en-US
            // TODO: test this !
            string culture = Get<string>(colladaDocument, "encoding", "en-US");
            encoding = new System.Globalization.CultureInfo(culture);

            // TODO: xmlns="http://www.collada.org/2005/11/COLLADASchema" version="1.4.1">
            XmlNode assetElement = root.SelectSingleNode("colladans:asset", nsmgr);
            coordinateSystem = new CoordinateSystem(CoordinateSystemType.RightHanded);
            if (assetElement != null)
            {
                asset = new Asset(this, assetElement);
                coordinateSystem.Up = ParseUpAxis(asset);
                
                Vector3 right = new Vector3();
                if (coordinateSystem.Up.X == 1f)
                    right.Y = -1f;
                else
                    right.X = 1f;
                
                coordinateSystem.Right = right;
                
                coordinateSystem.Meter = asset.meter;
            }

            // parse document for all libraries
            XmlNodeList imagesLibs = root.SelectNodes("colladans:library_images", nsmgr);
            
            foreach (XmlNode imagesLib in imagesLibs)
            {
                XmlNodeList imageElements = imagesLib.SelectNodes("colladans:image", nsmgr);
                foreach (XmlNode imageElement in imageElements)
                {
                    if (images == null) images = new List<Image>();
                    try
                    {
                        images.Add(new Image(this, imageElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList materialsLibs = root.SelectNodes("colladans:library_materials", nsmgr);
            materials = new List<Material>();
            foreach (XmlNode materialsLib in materialsLibs)
            {
                XmlNodeList materialElements = materialsLib.SelectNodes("colladans:material", nsmgr);
                foreach (XmlNode materialElement in materialElements)
                {
                    try
                    {
                        materials.Add(new Material(this, materialElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList effectsLibs = root.SelectNodes("colladans:library_effects", nsmgr);
            
            foreach (XmlNode effectsLib in effectsLibs)
            {
                XmlNodeList effectElements = effectsLib.SelectNodes("colladans:effect", nsmgr);
                foreach (XmlNode effectElement in effectElements)
                {
                    if (effects == null) effects = new List<Effect>();
                    try
                    {
                        effects.Add(new Effect(this, effectElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList geometryLibs = root.SelectNodes("colladans:library_geometries", nsmgr);
            
            foreach (XmlNode geometryLib in geometryLibs)
            {
                XmlNodeList geometryElements = geometryLib.SelectNodes("colladans:geometry", nsmgr);

                foreach (XmlNode geometryElement in geometryElements)
                {
                    if (geometries == null) geometries = new List<Geometry>();
                    try
                    {
                        geometries.Add(new Geometry(this, geometryElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                    catch (ColladaException e)
                    {
                        // most probably the geometry element defines a convex_mesh
                        // or spline geometry, which isn't supported -> ignore
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList controllerLibs = root.SelectNodes("colladans:library_controllers", nsmgr);
            
            foreach (XmlNode controllerLib in controllerLibs)
            {
                XmlNodeList controllerElements = controllerLib.SelectNodes("colladans:controller", nsmgr);

                foreach (XmlNode controllerElement in controllerElements)
                {
                    if (controllers == null) controllers = new List<Controller>();
                    try
                    {
                        controllers.Add(new Controller(this, controllerElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList nodeLibs = root.SelectNodes("colladans:library_nodes", nsmgr);
            
            foreach (XmlNode nodeLib in nodeLibs)
            {
                XmlNodeList nodeElements = nodeLib.SelectNodes("colladans:node", nsmgr);

                foreach (XmlNode nodeElement in nodeElements)
                {
                    if (nodes == null) nodes = new List<Node>();
                    try
                    {
                        nodes.Add(new Node(this, nodeElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList visualSceneLibs = root.SelectNodes("colladans:library_visual_scenes", nsmgr);
            
            foreach (XmlNode visualSceneLib in visualSceneLibs)
            {
                XmlNodeList visualSceneElements = visualSceneLib.SelectNodes("colladans:visual_scene", nsmgr);

                foreach (XmlNode visualSceneElement in visualSceneElements)
                {
                    if (visualScenes == null) visualScenes = new List<VisualScene>();
                    try
                    {
                        visualScenes.Add(new VisualScene(this, visualSceneElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                    catch (MissingRequiredElementException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }

            XmlNodeList animationLibs = root.SelectNodes("colladans:library_animations", nsmgr);
            animations = new List<Animation>();
            foreach (XmlNode animationLib in animationLibs)
            {
                XmlNodeList animationElements = animationLib.SelectNodes("colladans:animation", nsmgr);

                foreach (XmlNode animationElement in animationElements)
                {
                    try
                    {
                        animations.Add(new Animation(this, animationElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                        COLLADAUtil.Log(e);
                    }
                }
            }
            
            XmlNodeList cameraLibs = root.SelectNodes("colladans:library_cameras", nsmgr);
            cameras = new List<Camera>();
            foreach (XmlNode cameraLib in cameraLibs)
            {
                XmlNodeList cameraElements = cameraLib.SelectNodes("colladans:camera", nsmgr);

                foreach (XmlNode cameraElement in cameraElements)
                {
                    try
                    {
                    	cameras.Add(new Camera(this, cameraElement));
                    }
                    catch (NonUniqueIDException e)
                    {
                    	COLLADAUtil.Log(e);
                    }
                }
            }
            
            // Load the scene for display in the viewer

            XmlNode sceneElement = root.SelectSingleNode("colladans:scene", nsmgr);
            if (sceneElement != null)
            {
                foreach (XmlNode child in sceneElement.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "instance_visual_scene":
                            instanceVisualScene = new InstanceScene(this, child);
                            break;
                        case "instance_physics_scene":
                            instancePhysicsScene = new InstanceScene(this, child);
                            break;
                        default:
                            throw new ColladaException("un-expected <" + child.Name + "> in <scene> :" + filename);
                    }
                }
            }
            // release Xml document now that we have COLLADA in memmory
            root = null;
            colladaDocument = null;
        }
        
        private Vector3 ParseUpAxis(Document.Asset asset)
        {
            switch (asset.up_axis.ToUpper())
            {
                case "X_UP":
                    return new Vector3(1f, 0f, 0f);
                case "Z_UP":
                    return new Vector3(0f, 0f, 1f);
                default:
                    return new Vector3(0f, 1f, 0f);
            }
        }
    }
}

