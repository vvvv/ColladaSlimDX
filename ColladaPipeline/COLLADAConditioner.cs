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
using System.IO;
using System.Collections;
using System.Collections.Generic;

using VVVV.Collada.ColladaDocument;

#endregion

namespace VVVV.Collada.ColladaPipeline
{
    public class Conditioner
    {
        /// <summary>This method will convert convex polygons to triangles
        /// <para>A more advanced condionner would be required to handle convex, complex polygons</para>
        /// </summary>
        static public void ConvexTriangulator(Document doc)
        {
            foreach (Document.Geometry geo in doc.geometries)
            {
                List<Document.Primitive> triangles = new List<Document.Primitive>();
                foreach (Document.Primitive primitive in geo.mesh.primitives)
                {
                    if (primitive is Document.Polylist)
                    {
                        int triangleCount = 0;

                        foreach (int vcount in primitive.vcount) triangleCount += vcount - 2;
                        int[] newP = new int[primitive.stride * triangleCount * 3];
                        int count = 0;
                        int offset = 0;
                        int first = 0;
                        int last = 0;
                        int j, k;

                        foreach (int vcount in primitive.vcount)
                        {
                            first = offset;
                            last = first + 1;
                            for (j = 0; j < vcount - 2; j++)
                            {
                                // copy first
                                for (k = 0; k < primitive.stride; k++)
                                    newP[count++] = primitive.p[k + first * primitive.stride];
                                // copy previous last
                                for (k = 0; k < primitive.stride; k++)
                                    newP[count++] = primitive.p[k + last * primitive.stride];
                                last += 1;
                                // last = new point
                                for (k = 0; k < primitive.stride; k++)
                                    newP[count++] = primitive.p[k + last * primitive.stride];
                            }
                            offset = last + 1;
                        }
                        // Make a triangle out of this Polylist
                        Document.Triangle triangle = new Document.Triangle(doc, count / primitive.stride / 3, primitive.Inputs, newP);
                        triangle.name = primitive.name;
                        triangle.material = primitive.material;
                        triangle.extras = primitive.extras;
                        triangles.Add(triangle);
                    }
                    else if (primitive is Document.Triangle)
                    {
                        triangles.Add(primitive);
                    }
                    else if (primitive is Document.Line)
                    {
                        // remove lines for now...
                    }
                    else
                        throw new Exception("Unsurpoted primitive" + primitive.GetType().ToString() + " in Conditioner::ConvexTriangle");
                }
                geo.mesh.primitives = triangles;
            }
        }


        /// <summary>This method will create a single index for the primitives
        /// This will not work on polygons and polylist, since vcount is not taken in count there
        /// So you need to first run ConvexTriangulator (or equivalent) 
        /// <para>This will make it directly usable as a index vertexArray for drawing</para>
        /// </summary>
        static public void Reindexor(Document doc)
        {
            String[] channelFlags =
            {
                "POSITION",
                "NORMAL",
                "TEXCOORD",
                "COLOR",
                "TANGENT",
                "BINORMAL",
                "UV",
                "TEXBINORMAL",
                "TEXTANGENT"
            };

            Dictionary<string,int> channelCount = new Dictionary<string,int>();
            Dictionary<string, int> maxChannelCount = new Dictionary<string, int>();
            Dictionary<string, List<Document.Input>> inputs = new Dictionary<string, List<Document.Input>>(); ;



            foreach (Document.Geometry geo in doc.geometries)
            {
            	// Skip geometry if there are no primitives defined
            	if (geo.mesh.primitives.Count == 0)
            		continue;
            	
            	foreach (string i in channelFlags)
                	maxChannelCount[i] = 0;
            	
                // Check if all parts have the same vertex definition
                bool first=true;
                foreach (Document.Primitive primitive in geo.mesh.primitives)
                {
                    foreach (string i in channelFlags) 
                        channelCount[i] = 0;

                    foreach (Document.Input input in COLLADAUtil.GetAllInputs(primitive))
                    {
                        channelCount[input.semantic]++;
                    }
                    if (first)
                    {
                        foreach (string i in channelFlags)
                            if (maxChannelCount[i] < channelCount[i]) maxChannelCount[i] = channelCount[i];
                        first = false;
                    }
                     else
                    {
                        foreach (string i in channelFlags)
                            if (maxChannelCount[i] != channelCount[i])
                                throw new Exception("TODO:  mesh parts have different vertex buffer definition in geometry " + geo.id);
                    }
                }

                // create new float array and index
                List<List<int>> indexList = new List<List<int>>();
                List<int> indexes;
                List<float> farray = new List<float>();
                Dictionary<string,int> checkIndex = new Dictionary<string,int>();
                int index=0;

                foreach (Document.Primitive primitive in geo.mesh.primitives)
                {
                    foreach (string i in channelFlags) 
                        inputs[i] = new List<Document.Input>();

                    foreach (Document.Input input in COLLADAUtil.GetAllInputs(primitive))
                        inputs[input.semantic].Add(input);

                    indexes = new List<int>();
                    indexList.Add(indexes);

                    int k=0;
                    string indexKey;
                    List<float> tmpValues;
                    try
                    {
                        while (true)
                        {
                            indexKey = "";
                            tmpValues = new List<float>();
                            foreach (string i in channelFlags)
                                foreach (Document.Input input in inputs[i])
                                {
                                    int j = COLLADAUtil.GetPValue(input, primitive, k);
                                    indexKey += j.ToString() + ",";
                                    float[] values = COLLADAUtil.GetSourceElement(doc,input, j);
                                    for (int l = 0; l < values.Length; l++) tmpValues.Add(values[l]);
                                }
                            k++;
                            if (checkIndex.ContainsKey(indexKey))
                                indexes.Add(checkIndex[indexKey]);
                            else
                            {
                                indexes.Add(index);
                                checkIndex[indexKey] = index++;
                                foreach (float f in tmpValues) farray.Add(f);
                            }
                        }
                    }
                    catch { } // catch for index out of range.
                }
                // remove old sources and array
                foreach (Document.Source source in geo.mesh.sources)
                {
                    if (source.array != null)
                        doc.dic.Remove(((Document.Array<float>)source.array).id);
                    doc.dic.Remove(source.id);
                }

                // create all the new source
                int stride = 0;
                foreach (Document.Source source in geo.mesh.sources)
                {
                    stride += source.accessor.stride;
                }

                List<Document.Source> newSources = new List<Document.Source>();
                Document.Source newSource;
                Document.Accessor newAccessor;
                Document.Array<float> newArray;
                int offset = 0;
                string positionId = ((Document.Source)inputs["POSITION"][0].source).id;
                foreach (Document.Source source in geo.mesh.sources)
                {
                    newAccessor = new Document.Accessor(doc, farray.Count / stride, offset, stride, "#"+geo.id + "-vertexArray", source.accessor.parameters);
                    offset += source.accessor.stride;
                    if (source.id == positionId)
                    {
                        newArray = new Document.Array<float>(doc, geo.id + "-vertexArray", farray.ToArray());
                    }
                    else
                    {
                        newArray = null;
                    }
                    newSource = new Document.Source(doc, source.id, newArray, newAccessor);
                    newSources.Add(newSource);
                }

                // Create the new vertices
                List<Document.Input> newInputs = new List<Document.Input>();
                Document.Input newInput;
                foreach (string i in channelFlags)
                {
                    foreach (Document.Input input in inputs[i])
                    {
                        // no offset, all inputs share the same index
                        newInput = new Document.Input(doc, 0, input.semantic, input.set, ((Document.Source)input.source).id);
                        newInputs.Add(newInput);
                    }
                }
                Document.Vertices newVertices = new Document.Vertices(doc, geo.mesh.vertices.id, newInputs);

                // now create the new primitives
                List<Document.Primitive> newPrimitives = new List<Document.Primitive>();
                Document.Primitive newPrimitive;

                index = 0;
                offset = 0;
                foreach (Document.Primitive primitive in geo.mesh.primitives)
                {

                    newInputs = new List<Document.Input>();
                    newInput = new Document.Input(doc, 0, "VERTEX", -1, geo.mesh.vertices.id);
                    newInputs.Add(newInput);

                    if (primitive is Document.Triangle)
                        newPrimitive = new Document.Triangle(doc, primitive.count, newInputs, indexList[index].ToArray());
                    else if (primitive is Document.Line)
                        newPrimitive = new Document.Line(doc, primitive.count, newInputs, indexList[index].ToArray());
                    else
                        throw new Exception("TODO: need to take care of " + primitive.GetType().ToString());
                    newPrimitive.material = primitive.material;
                    newPrimitive.extras = primitive.extras;
                    newPrimitive.name = primitive.name;
                    newPrimitives.Add(newPrimitive);

                    index++;
                }
                
                // change the primitive to use the new array and indexes.
       
                // 1) - remove the old sources, vertices and primitives

                geo.mesh.sources.Clear();
                geo.mesh.vertices.inputs.Clear();
                geo.mesh.primitives.Clear();

                // 2) - Add all the sources, only the POSITION will have the values

                geo.mesh.sources = newSources;
                geo.mesh.primitives = newPrimitives;
                geo.mesh.vertices = newVertices;

            } // foreach geometry
        } // Reindexor()

    } // Conditionner class
} // namespace COLLADA

 
