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

#region using statements
using System;
using System.Collections.Generic;
using System.Text;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Collada.ColladaDocument;
//using Microsoft.Xna.Framework; // Vector3
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Content; // ContentManager
//using Microsoft.Xna.Framework.Content.Pipeline; // ExternalReference
//using Microsoft.Xna.Framework.Content.Pipeline.Graphics; //BasicMaterial

#endregion

namespace VVVV.Collada.ColladaModel
{
 
    [Serializable()]
    public class Model
    {
		#region inner classes
        [Serializable()]
        public class CMeshPart 
        {
            // Summary:
            //     Gets the offset to add to each vertex index in the index buffer.
            //
            // Returns:
            //     Offset to add to each vertex index in the index buffer.
            public int BaseVertex { get { return baseVertex; } }
            private int baseVertex;
            //
            // provided by InstanceMesh
            //public Effect Effect { get { return effect; } set { effect = value; } }
            //private Effect effect;

            //
            // Summary:
            //     Get the material symbol for this mesh part.
            //
            // Returns:
            //     The material symbol for the binding of this part
            public string Material { get { return material; } set { material = value; } }
            private string material;
            //
            // Summary:
            //     Gets the number of vertices used during a draw call.
            //
            // Returns:
            //     The number of vertices used during the call.
            public int NumVertices { get { return numVertices; } }
            private int numVertices;
            //
            // Summary:
            //     Gets the number of primitives to render.
            //
            // Returns:
            //     The number of primitives to render. The number of vertices used is a function
            //     of primitiveCount and primitiveType.
            public int PrimitiveCount { get { return primitiveCount; } }
            private int primitiveCount;
            //
            // Summary:
            //     Gets the location in the index array at which to start reading vertices.
            //
            // Returns:
            //     Location in the index array at which to start reading vertices.
            public int StartIndex { get { return startIndex; } }
            private int startIndex;
            //
            // Summary:
            //     Gets the offset in bytes from the beginning of the VertexBuffer.
            //
            // Returns:
            //     The offset in bytes from the beginning of the VertexBuffer.
            public short StreamOffset { get { return streamOffset; } }
            private short streamOffset;
            //
            // Summary:
            //     Gets or sets an object identifying this model mesh part.
            //
            // Returns:
            //     An object identifying this model mesh part.
            public Object Tag { get { return tag; } set { tag = value; } }
            private Object tag = null;
            
            public List<VertexElement> VertexElements { get { return vertexElements; } }
            private List<VertexElement> vertexElements;
            //
            // Summary:
            //     Gets the size, in bytes, of the elements in this vertex stream.
            //
            // Returns:
            //     The size, in bytes, of the elements in this vertex stream.
            public short VertexStride { get { return vertexStride; } }
            private short vertexStride;
            //
            // Summary:
            //     Gets the primitive type
            //
            // Returns:
            //     PrimitiveType enum
            public PrimitiveType PrimitiveType { get { return primitiveType; } }
            private PrimitiveType primitiveType;

            private int[] indexArray;
            public int[] IndexArray { get { return indexArray; } }
            
            //
            // Summary:
            //     Create a new Model Mesh Part.
            public CMeshPart(Document doc, Document.Primitive primitive)
            {
                material = primitive.material;

                if (primitive is Document.Triangle)
                    primitiveType = PrimitiveType.TriangleList;
                else if (primitive is Document.Line)
                    primitiveType = PrimitiveType.LineList;
                else
                    throw new Exception("Unexpected primitiveType=" + primitive.GetType().ToString());

                primitiveCount = primitive.count; // number of primitives to draw
                streamOffset = 0; // selection which input stream to use
                startIndex = 0; // first index element to read
                baseVertex = 0; // vertex buffer offset to add to each element of the index buffer
                vertexStride = 0;

                vertexElements = new List<VertexElement>();
                short deltaOffset = 0;
                short offset = 0;


                DeclarationType vertexElementFormat;
                DeclarationUsage vertexElementUsage;
                byte usageIndex;

                foreach (Document.Input input in COLLADAUtil.GetAllInputs(primitive))
                {

                    switch (input.semantic)
                    {
                        case "POSITION":
                            vertexElementUsage = DeclarationUsage.Position;
                            usageIndex = 0;
                            // number of vertices in mesh part
                            numVertices = ((Document.Source)input.source).accessor.count;
                            break;
                        case "NORMAL":
                            vertexElementUsage = DeclarationUsage.Normal;
                            usageIndex = 0;
                            break;
                        case "COLOR":
                            vertexElementUsage = DeclarationUsage.Color;
                            usageIndex = 0;
                            break;
                        case "TEXCOORD":
                            vertexElementUsage = DeclarationUsage.TextureCoordinate;
                            usageIndex = 0; // TODO handle several texture (need to replace BasicMaterial first)
                            break;
                        case "TEXTANGENT":
                        case "TEXBINORMAL":
                            vertexElementUsage = DeclarationUsage.Color; // Whatever, just for C# to stop complaining
                            usageIndex = 0; // Whatever
                            break;
                        default:
                            throw new Exception("Unexeptected vertexElementUsage=" + input.semantic);
                    }

                    // assuming floats !
                    switch (((Document.Source)input.source).accessor.ParameterCount)
                    {
                        case 2:
                            vertexElementFormat = DeclarationType.Float2;
                            deltaOffset = 2 * 4;
                            vertexStride += 2;
                            break;
                        case 3:
                            vertexElementFormat = DeclarationType.Float3;
                            deltaOffset = 3 * 4;
                            vertexStride += 3;
                            break;
                        default:
                            throw new Exception("Unexpected vertexElementFormat");

                    }
                    
                    vertexElements.Add(new VertexElement(streamOffset /* stream */,
                                                         offset  /* offset */,
                                                         vertexElementFormat,
                                                         DeclarationMethod.Default,
                                                         vertexElementUsage,
                                                         usageIndex));

                    offset += deltaOffset;
                } // foreach input

                indexArray = new int[primitive.p.Length];

                if (primitive is Document.Triangle)
                {
                	// reverse triangle order for directX
                    for (int i = 0; i < primitive.p.Length; i += 3)
                    {
                		indexArray[i] = primitive.p[i + 2];
                        indexArray[i + 1] = primitive.p[i + 1];
                        indexArray[i + 2] = primitive.p[i];
                    }
                }
                else
                	Array.Copy(primitive.p, indexArray, primitive.p.Length);
                
            }
        } // ModelMeshPart

        // Summary:
        //     Represents a mesh that is part of a Model.
        [Serializable()]
        public class CMesh
        {
        	private Document.Geometry geo;
        	public Document.Geometry Geo { get { return geo; } }
        	
            public List<Document.Primitive> Primitives { get { return primitives; } }
            private List<Document.Primitive> primitives = new List<Document.Primitive>();
            
            private float[] vertexArray;
            public float[] VertexArray { get { return vertexArray; } }
            
            private int[] indexArray;
            public int[] IndexArray { get { return indexArray; } }
            
            public List<VertexElement> VertexElements { get { return vertexElements; } }
            private List<VertexElement> vertexElements = new List<VertexElement>();
            
            private int faceCount = 0;
            public int FaceCount { get { return faceCount; } }
            
            private int vertexCount = 0;
            public int VertexCount { get { return vertexCount; } }
            
            private short vertexStride = 0;
            public short VertexStride { get { return vertexStride; } }
            
            // Describes a mapping from final vertex buffer position to original
            // position in vertex source array defined by collada.
            private int[] invVertexMapping;
            protected int[] InvVertexMapping { get { return invVertexMapping; } }
     
            public CMesh(Model model, Document.Geometry geo) 
            {
            	this.geo = geo;
                Document doc = model.Doc;

                bool first = true;
                
                int vertexIndex = 0;
                List<int> indexBuffer = new List<int>();
                List<float> vertexBuffer = new List<float>();
                List<int> invVertexMappingList = new List<int>();
                Dictionary<string, int> mapping = new Dictionary<string, int>();
                List<float> tempValues = new List<float>();
                foreach (Document.Primitive primitive in geo.mesh.primitives)
                {
                	// Skip non-triangle primitives
                	if (!(primitive is Document.Triangle))
                	{
                		// Display a warning
                		COLLADAUtil.Log(COLLADALogType.Warning, "Skipping part of geometry '" + geo.id + "'! Make sure all primitives are triangles.");
                		continue;
                	}
                	
                	#region vertex declaration
                	// Take the first primitive and use its inputs to build the 
		            // vertex declaration and a dictionary to describe a mapping
		            // of n source/p_index pairs to one index in the vertex buffer.
		            // TODO: for now we assume that all primitives have the same
		            // inputs in equal order.
		            if (first)
		            {
		            	first = false;
		            	
		            	short deltaOffset = 0;
		                short offset = 0;
		                short streamOffset = 0;
		
		                DeclarationType vertexElementFormat;
		                DeclarationUsage vertexElementUsage;
		                byte usageIndex;
		            	foreach (Document.Input input in COLLADAUtil.GetAllInputs(primitive))
		            	{
		                    switch (input.semantic)
		                    {
		                        case "POSITION":
		                            vertexElementUsage = DeclarationUsage.Position;
		                            usageIndex = 0;
		                            break;
		                        case "NORMAL":
		                            vertexElementUsage = DeclarationUsage.Normal;
		                            usageIndex = 0;
		                            break;
		                        case "COLOR":
		                            vertexElementUsage = DeclarationUsage.Color;
		                            usageIndex = 0;
		                            break;
		                        case "TEXCOORD":
		                            vertexElementUsage = DeclarationUsage.TextureCoordinate;
		                            usageIndex = 0; // TODO handle several texture (need to replace BasicMaterial first)
		                            break;
		                        case "TEXTANGENT":
		                        case "TEXBINORMAL":
		                            vertexElementUsage = DeclarationUsage.Color; // Whatever, just for C# to stop complaining
		                            usageIndex = 0; // Whatever
		                            break;
		                        default:
		                            throw new Exception("Unexeptected vertexElementUsage=" + input.semantic);
		                    }
		
		                    // assuming floats !
		                    switch (((Document.Source)input.source).accessor.ParameterCount)
		                    {
		                        case 2:
		                            vertexElementFormat = DeclarationType.Float2;
		                            deltaOffset = 2 * 4;
		                            vertexStride += 2;
		                            break;
		                        case 3:
		                            vertexElementFormat = DeclarationType.Float3;
		                            deltaOffset = 3 * 4;
		                            vertexStride += 3;
		                            break;
		                        default:
		                            throw new Exception("Unexpected vertexElementFormat");
		
		                    }
		                    
		                    vertexElements.Add(new VertexElement(streamOffset /* stream */,
		                                                         offset  /* offset */,
		                                                         vertexElementFormat,
		                                                         DeclarationMethod.Default,
		                                                         vertexElementUsage,
		                                                         usageIndex));
		
		                    offset += deltaOffset;
		            	}
		            }
		            #endregion
		            
                	// Build vertex and index array
                	for (int v = 0; v < primitive.count * 3; v++)
                	{
                		int positionIndex = 0;
                		string indexKey = "";
                		tempValues.Clear();
                		
                		foreach (Document.Input input in COLLADAUtil.GetAllInputs(primitive))
                		{
                			// Get index into source array of this input for this triangle t
                			int index = COLLADAUtil.GetPValue(input, primitive, v);
                			
                			// Save index of position array for later inverse mapping
                			if (input.semantic == "POSITION")
                				positionIndex = index;
                			
                			// Build key out of all indices for later lookup
                			indexKey += index + ",";
                			// Get values this index is referencing
                			float[] values = COLLADAUtil.GetSourceElement(doc, input, index);
                			// Add the values to temporary storage
                			tempValues.AddRange(values);
                		}
                		
                		// Add this combination of inputs to the vertex buffer
                		if (mapping.ContainsKey(indexKey))
                			indexBuffer.Add(mapping[indexKey]);
                		else
                		{
                			mapping[indexKey] = vertexIndex;
                			indexBuffer.Add(vertexIndex);
                			vertexBuffer.AddRange(tempValues);
                			// Inverse mapping of vertex buffer index to index of original array
                			invVertexMappingList.Add(positionIndex);
                			// Increment index of vertex buffer
                			vertexIndex++;
                		}
                	}
                	
                	primitives.Add(primitive);
                	
                	faceCount += primitive.count;
                }
                
                vertexCount = vertexIndex;
                
                indexArray = indexBuffer.ToArray();
                vertexArray = vertexBuffer.ToArray();
                invVertexMapping = invVertexMappingList.ToArray();
                
                // Reverse triangle order for DirectX
                for (int i = 0; i < indexArray.Length; i += 3)
                {
                	int swap = indexArray[i];
                	indexArray[i] = indexArray[i + 2];
                	indexArray[i + 2] = swap;
                }
                
                // Reverse texture 'T' coordinate for DirectX
                foreach (VertexElement vertexElement in VertexElements)
                {
                    if (vertexElement.Usage == DeclarationUsage.TextureCoordinate)
                    {
                        for (int i = 0; i < vertexCount; i++)
                            vertexArray[i * VertexStride + vertexElement.Offset / 4 + 1] =
                                1.0f - vertexArray[i * VertexStride + vertexElement.Offset / 4 + 1];
                    }
                }
                
                FinalizeVertexDeclaration(vertexElements);
            }
            
            public virtual Mesh Create3D9Mesh(Device graphicsDevice, ref int attribId)
            {
            	Mesh mesh = new Mesh(graphicsDevice, FaceCount, VertexCount, MeshFlags.Use32Bit | MeshFlags.Managed, VertexElements.ToArray());
            	fillVertexBuffer(mesh);
            	fillIndexBuffer(mesh);
            	fillAttributeBuffer(mesh, ref attribId);
            	mesh.OptimizeInPlace(MeshOptimizeFlags.AttributeSort);

            	return mesh;
            }
            
            protected virtual void FinalizeVertexDeclaration(List<VertexElement> vertexElements)
            {
            	vertexElements.Add(VertexElement.VertexDeclarationEnd);
            }
            
            protected virtual void fillVertexBuffer(Mesh mesh)
            {
            	DataStream ds = mesh.LockVertexBuffer(LockFlags.None);
            	ds.WriteRange(VertexArray);
            	mesh.UnlockVertexBuffer();
            }
            
            protected virtual void fillIndexBuffer(Mesh mesh)
            {
            	DataStream ds = mesh.LockIndexBuffer(LockFlags.None);
            	ds.WriteRange(IndexArray);
            	mesh.UnlockIndexBuffer();
            }
            
            protected virtual void fillAttributeBuffer(Mesh mesh, ref int attribId)
            {
            	List<int> attribList = new List<int>();
            	
            	int faceStart = 0;
            	int faceEnd = 0;
            	
            	foreach (Document.Primitive primitive in Primitives)
            	{
            		faceEnd = faceStart + primitive.count;
            		for (int i = faceStart; i < faceEnd; i++)
            			attribList.Add(attribId);
            		
            		attribId++;
            		faceStart = faceEnd;
            	}
            	
            	DataStream ds = mesh.LockAttributeBuffer(LockFlags.None);
            	ds.WriteRange(attribList.ToArray());
            	mesh.UnlockAttributeBuffer();
            }
            
			public override string ToString()
			{
				if (geo != null)
					return geo.id;
				return base.ToString();
			}
        } // Mesh
        
        public class CSkinnedMesh : CMesh
        {
        	
        	public static byte PaletteSize = 32;
        	
        	public List<Dictionary<short, byte>> PaletteList { get { return paletteList; } }
        	private List<Dictionary<short, byte>> paletteList = new List<Dictionary<short, byte>>();
        	
        	public Model Model { get { return model; } }
        	private Model model;
        	
        	public Document.Skin Skin { get { return skin; } }
        	private Document.Skin skin;
        	private Document.Geometry geo;
        	
        	public int BoneCount { get { return boneCount; } }
        	private int boneCount = 0;
        	
        	public CSkinnedMesh(Model model, Document.Geometry geo, Document.Skin skin) 
        		: base(model, geo)
        	{
        		this.geo = geo;
        		this.model = model;
        		this.skin = skin;
        		
        		/*
        		foreach (Document.Input input in skin.joint.inputs)
        		{
        			if (input.semantic == "JOINT")
        			{
        				Document.Source source = (Document.Source) input.source;
        				boneCount = ((Document.Array<string>) source.array).Count;
        			}
        		}
        		*/
        	}
        	
        	protected override void FinalizeVertexDeclaration(List<VertexElement> vertexElements)
        	{
        		short offset = 0;
        		foreach (VertexElement ve in vertexElements)
        		{
        			switch (ve.Type)
        			{
        				case DeclarationType.Float2:
        					offset += 2 * 4;
        					break;
        				case DeclarationType.Float3:
        					offset += 3 * 4;
        					break;
        				default:
        					throw new ColladaException("TODO: support " + ve.Type.ToString() + " in vertex declaration of CSkinnedMesh!");
        			}
        		}
    			
    			vertexElements.Add(
    				new VertexElement(0,
                                      offset,
                                      DeclarationType.Ubyte4,
                                      DeclarationMethod.Default,
                                      DeclarationUsage.BlendIndices,
                                      0));
    			
    			offset += 1 * 4;
				vertexElements.Add(
    				new VertexElement(0,
                                      offset,
                                      DeclarationType.Float4,
                                      DeclarationMethod.Default,
                                      DeclarationUsage.BlendWeight,
                                      0));
    			
    			vertexElements.Add(VertexElement.VertexDeclarationEnd);
        	}
        	
        	protected override void fillVertexBuffer(Mesh mesh)
            {
        		short[] blendIndices = new short[4];
        		byte[] blendIndicesVB = new byte[4];
        		float[] blendWeights = new float[4];
        		float blendWeight, blendWeightSum;
        		bool displayMaxNumInfluenceWarning = true;
        		
        		// get weight input element
        		Document.Input weightInput = null;
        		foreach (Document.Input input in skin.vertexWeights.inputs)
        			if (input.semantic == "WEIGHT")
        				weightInput = input;
        		
            	DataStream ds = mesh.LockVertexBuffer(LockFlags.None);
            	try 
            	{
            		// Read out all blend weights and indices to build an offset table
            		uint[] offsets = new uint[skin.vertexWeights.count];
            		uint offset = 0;
            		for (int i = 0; i < offsets.Length; i++)
            		{
            			offsets[i] = offset;
            			offset += 2 * skin.vertexWeights.vcount[i];
            		}
            		
	            	for (int vertexIndex = 0; vertexIndex < VertexCount; vertexIndex++)
	            	{
	            		ds.WriteRange(VertexArray, vertexIndex * VertexStride, VertexStride);
	            		
	            		int v = InvVertexMapping[vertexIndex];
	            		offset = offsets[v];
	            		
	            		blendWeightSum = 0;
	            		for (int j = 0; j < 4; j++)
	            		{
	            			if (j < skin.vertexWeights.vcount[v])
	            			{
	            				blendIndices[j] = (short) skin.vertexWeights.v[offset + 2 * j];
	            				blendIndicesVB[j] = (byte) blendIndices[j];
	            				blendWeight = COLLADAUtil.GetSourceElement(
		            				model.Doc, 
		            				weightInput, 
		            				skin.vertexWeights.v[offset + 2 * j + 1])[0];
	            				blendWeights[j] = blendWeight;
		            			blendWeightSum += blendWeight;
	            			}
	            			else
	            			{
	            				blendIndices[j] = 0;
	            				blendIndicesVB[j] = 0;
	            				blendWeights[j] = 0;
	            			}
	            		}
	            		
	            		
	            		// Find palette or create a new one
	            		Dictionary<short, byte> palette = FindPalette(blendIndices, blendWeights);
            			// Reindex
        				for (int j = 0; j < 4; j++)
            			{
        					if (blendWeights[j] > 0)
		            			blendIndices[j] = palette[blendIndices[j]];
            			}
	            		
	            		// There are only up to four influences allowed.
	            		// TODO: Renormalize if there are more. For now show a warning.
	        			if (displayMaxNumInfluenceWarning && skin.vertexWeights.vcount[v] > 4)
	        			{
	        				COLLADAUtil.Log(COLLADALogType.Warning, "There are only up to four influences allowed. Skinning might be incorrect.");
	        				displayMaxNumInfluenceWarning = false;
	        			}
	        			
	        			// renormalize
	        			if (blendWeightSum > 0) 
	        			{
	        				for (int j = 0; j < 4; j++) 
	        				{
	        					blendWeights[j] = blendWeights[j] / blendWeightSum;
	        				}
	        			}
	        			
	        			ds.WriteRange(blendIndicesVB);
	        			ds.WriteRange(blendWeights);
	            	}
            	} 
            	catch (Exception e)
            	{
            		COLLADAUtil.Log(COLLADALogType.Error, e.Message + e.StackTrace);
            	}
            	finally
            	{
            		mesh.UnlockVertexBuffer();
            	}
            	COLLADAUtil.Log("palette count: " + PaletteList.Count);
            }
        	
        	private Dictionary<short, byte> FindPalette(short[] blendIndices, float[] blendWeights)
        	{
        		Dictionary<short, byte> paletteWithRoom = null;
        		
        		foreach (Dictionary<short, byte> palette in PaletteList)
        		{
        			bool result = true;
        			byte missingIndices = 0;
        			for (int i = 0; i < blendIndices.Length; i++)
        			{
        				if (blendWeights[i] > 0)
        				{
        					bool found = palette.ContainsKey(blendIndices[i]);
        					if (!found)
        						missingIndices++;
        					result = result && found;
        				}
        			}
        			
        			if (result)
        				return palette;
        			else
        			{
        				// This palette didn't match, but it could hold enough
        				// room for later storage.
        				if (paletteWithRoom == null && PaletteSize - palette.Count > missingIndices)
        					paletteWithRoom = palette;
        			}
        		}
        		
        		// We didn't find a matching palette.
        		if (paletteWithRoom == null)
        		{
        			paletteWithRoom = new Dictionary<short, byte>();
        			PaletteList.Add(paletteWithRoom);
        		}
        		
        		// Now add the missing indices and return it
        		byte offset = (byte) paletteWithRoom.Count;
        		for (int j = 0; j < 4; j++)
				{
        			if (blendWeights[j] > 0 && !paletteWithRoom.ContainsKey(blendIndices[j]))
						paletteWithRoom.Add(blendIndices[j], offset++);
				}
        		
        		return paletteWithRoom;
        	}
        }

        // Summary:
        //     Represents bone data for a model.
        [Serializable]
        public class Bone
        {
            // Summary:
            //     Gets a collection of bones that are children of this bone.
            //
            // Returns:
            //     A collection of bones that are children of this bone.
            public List<Model.Bone> Children { get { return children; } }
            private List<Model.Bone> children;
            //
            // Summary:
            //     Gets the index of this bone in the Model.Bones collection.
            //
            // Returns:
            //     The index of this bone in the Model.Bones collection.
            public int Index { get { return index; } }
            private int index;
            //
            // Summary:
            //     Gets the name of this bone.
            //
            // Returns:
            //     The name of this bone.
            public string Name { get { return name;} }
            private string name;
            //
            // Summary:
            //     Gets the parent of this bone.
            //
            // Returns:
            //     The parent of this bone.
            public Bone Parent { get { return parent; } }
            private Bone parent;
            //
            // Summary:
            //     Gets the matrix used to transform this bone.
            //
            // Returns:
            //     The matrix used to transform this bone.
            public Matrix TransformMatrix { 
            	get 
            	{
            		Matrix m = Matrix.Identity;
            		foreach (Transform t in Transforms.Values)
            		{
            			m = t.Matrix * m;
            		}
            		return m;
            	}
            }
            
            public Matrix AbsoluteTransformMatrix {
            	get
            	{
            		if (Parent == null)
            			return TransformMatrix;
            		
            		return TransformMatrix * Parent.AbsoluteTransformMatrix;
            	}
            }
            
            private Dictionary<string, Transform> transforms;
            public Dictionary<string, Transform> Transforms { get { return transforms; } }

            //
            // Summary:
            //     Create a new Bone
            //
            // Returns:
            //     Returns the new bone, having updated the bone list in the model, and the index for this bone
            private Bone() { }
            public Bone(Model model, Bone _parent, string _name) 
            {
            	this.transforms = new Dictionary<string, Transform>();
            	model.BonesTable.Add(_name, this);
                this.index = model.Bones.Count-1;
                this.children = new List<Bone>();
                name = _name;
                parent = _parent;
            }
            
            public Matrix AbsoluteTransformMatrixRoot(Bone rootBone) {
        		if (Parent == null || rootBone == this)
        			return TransformMatrix;
        		
        		return TransformMatrix * Parent.AbsoluteTransformMatrix;
            }

        } // ModelBone
        
        #region transform classes
        public class Transform
		{
        	private static Transform identity = new Transform();
        	public static Transform Identity { get { return identity; } }
        	private List<Animation.Channel> channels;
        	
        	protected Matrix matrix;
        	
        	public Transform()
	        {
	        	this.matrix = Matrix.Identity;
        	}
        	
        	public Transform(Matrix m)
        	{
        		this.matrix = m;
        	}
        	
        	public Matrix Matrix { get { return matrix; } }
        	public virtual void SetElement(string key, float[] val) {
        		throw new Exception("method not implemented yet! if you get this exception then a specialized implementation of class Transform, which can handle the setElement method properly, is missing.");
        	}
        	
        	public void AddChannel(Animation.Channel channel)
        	{
        		if (channels == null)
        			channels = new List<Animation.Channel>();
        		channels.Add(channel);
        	}
        	
        	public void ApplyAnimations(float time)
        	{
        		if (channels == null)
        			return;
        		
        		foreach (Animation.Channel channel in channels)
        		{
        			channel.Apply(time);
        		}
        	}
        }
        
        public class RotateTransform : Transform
        {
        	private Vector3 axis;
        	private float angle;
        	
        	// angle in radiant
        	public RotateTransform(Vector3 axis, float angle)
        	{
        		this.axis = axis;
        		this.angle = angle * DegToRad;
        		this.matrix = Matrix.RotationAxis(axis, this.angle);
        	}
        	
        	public override void SetElement(string key, float[] val) {
        		if (key == "ANGLE")
        		{
        			this.angle = val[0] * DegToRad;
        			this.matrix = Matrix.RotationAxis(axis, angle);
        		}
        		else
        			throw new ColladaException("Unknown key " + key + " in channel target");
        	}
        }
        
        public class TranslateTransform : Transform
        {
        	private Vector3 amount;
        	
        	public TranslateTransform(Vector3 amount)
        	{
        		this.amount = amount;
        		this.matrix = Matrix.Translation(amount);
        	}
        	
        	public override void SetElement(string key, float[] val) {
        		if (key == null)
        		{
        			amount.X = val[0];
        			amount.Y = val[1];
        			amount.Z = val[2];
        		}
        		else if (key == "X")
        			amount.X = val[0];
        		else if (key == "Y")
        			amount.Y = val[0];
        		else if (key == "Z")
        			amount.Z = val[0];
        		else
        			throw new ColladaException("Unknown key " + key + " in channel target");
        		this.matrix = Matrix.Translation(amount);
        	}
        }
        
        public class ScaleTransform : Transform
        {
        	private Vector3 scale;
        	
        	public ScaleTransform(Vector3 scale)
        	{
        		this.scale = scale;
        		this.matrix = Matrix.Scaling(scale);
        	}
        	
        	public override void SetElement(string key, float[] val) {
        		if (key == null)
        		{
        			scale.X = val[0];
        			scale.Y = val[1];
        			scale.Z = val[2];
        		}
        		else if (key == "X")
        			scale.X = val[0];
        		else if (key == "Y")
        			scale.Y = val[0];
        		else if (key == "Z")
        			scale.Z = val[0];
        		else
        			throw new ColladaException("Unknown key " + key + " in channel target");
        		this.matrix = Matrix.Scaling(scale);
        	}
        }
        #endregion
        
        public class Animation
        {
        	
			public class Channel
			{
				private Sampler sampler;
				private Transform transform;
        		private string targetTransformKey;
        		
				public Channel(Document doc, Model model, Document.Channel channel, Dictionary<string, Sampler> samplers)
				{
					if (!samplers.TryGetValue(channel.source.id, out sampler))
						throw new ColladaException("can't find source with id " + channel.source.id + " of <channel>");
					
					Bone bone;
					string targetNodeId = COLLADAUtil.getTargetNodeId(doc, channel.target);
					if (!model.BonesTable.TryGetValue(targetNodeId, out bone))
						throw new ColladaException("can't find target node in <channel>");
					
					string[] tmp = channel.target.Split('/');
					tmp = tmp[tmp.Length - 1].Split('.');
					string targetTransformId = tmp[0];
					if (tmp.Length == 1)
						targetTransformKey = null;
					else
						targetTransformKey = tmp[1];
					
					if (!bone.Transforms.TryGetValue(targetTransformId, out transform))
						throw new ColladaException("can't find target transformation '" + targetTransformId + "' in <channel>");
					
					transform.AddChannel(this);
				}
				
				public void Apply(float time)
        		{
					float[] val = sampler.sample(time);
        			transform.SetElement(targetTransformKey, val);
        		}
			}
			
			public class Sampler
			{
				public static double APPROXIMATION_EPSILON = 1.0e-09;
				public static double VERYSMALL = 1.0e-20;
				public static int MAXIMUM_ITERATIONS = 1000;
				
				public enum EInterpolation { LINEAR, BEZIER, CARDINAL, HERMITE, BSPLINE, STEP };
				private EInterpolation[] interpolations;
				private float[] inputs;
				private Document.Input output;
				private Document.Input inTangents;
				private Document.Input outTangents;
				private int dim_tangents;
				private int dim_output;
				private float startTime;
				private float endTime;
				private Document doc;
				
				public Sampler(Document doc, Document.Sampler sampler)
				{
					foreach (Document.Input input in sampler.inputs)
					{
						this.doc = doc;
						Document.Source source = input.source as Document.Source;
						switch (input.semantic)
						{
							case "INTERPOLATION":
								Document.Array<string> arr = source.array as Document.Array<string>;
								interpolations = new EInterpolation[arr.Count];
								for (int i = 0; i < interpolations.Length; i++)
								{
									switch (arr.arr[i])
									{
										case "LINEAR":
											interpolations[i] = EInterpolation.LINEAR;
											break;
										case "BEZIER":
											interpolations[i] = EInterpolation.BEZIER;
											break;
										case "STEP":
											interpolations[i] = EInterpolation.STEP;
											break;
										default:
											throw new ColladaException("unsupported interpolation type " + arr.arr[i] + " in <sampler>!"); // TODO: implement this
									}
								}
								break;
							case "INPUT":
								inputs = (source.array as Document.Array<float>).arr;
								startTime = inputs[0];
								endTime = inputs[inputs.Length - 1];
								break;
							case "OUTPUT":
								output = input;
								dim_output = ((Document.Source) output.source).accessor.ParameterCount;
								break;
							case "IN_TANGENT":
								inTangents = input;
								dim_tangents = ((Document.Source) inTangents.source).accessor.ParameterCount;
								break;
							case "OUT_TANGENT":
								outTangents = input;
								break;
							default:
								throw new ColladaException("unsupported semantic " + input.semantic + " in <sampler>!");
						}
					}
					
					if (interpolations == null)
					{
						// some examples don't specify required input element with semantic INTERPOLATION
						// TODO: write a warning to log
						interpolations = new EInterpolation[inputs.Length];
						for (int i = 0; i < interpolations.Length; i++)
							interpolations[i] = EInterpolation.LINEAR;
					}
				}
				
				public float[] sample(float time)
				{
					if (time <= startTime)
						return COLLADAUtil.GetSourceElement(doc, output, 0);
					if (time >= endTime)
						return COLLADAUtil.GetSourceElement(doc, output, inputs.Length - 1);
					
					int i = findKeyFrame(time);
					int j = i + 1;
					
					float[][] outp = new float[2][];
					outp[0] = COLLADAUtil.GetSourceElement(doc, output, i);
					outp[1] = COLLADAUtil.GetSourceElement(doc, output, j);
					
					float s;
					float[] result;
					switch (interpolations[i])
					{
						case EInterpolation.STEP:
							return outp[0];
						case EInterpolation.LINEAR:
							s = (time - inputs[i]) / (inputs[j] - inputs[i]);
							result = new float[dim_output];
							for (int k = 0; k < dim_output; k++)
								result[k] = (float) (outp[0][k] + (outp[1][k] - outp[0][k]) * s);
							return result;
						case EInterpolation.BEZIER:
							float[][] tang = new float[2][];
							tang[0] = COLLADAUtil.GetSourceElement(doc, outTangents, i);
							tang[1] = COLLADAUtil.GetSourceElement(doc, inTangents, j);
							if (dim_output == dim_tangents)
							{
								float[][] p = new float[2][];
								float[][] c = new float[2][];
								result = new float[dim_output];
								
								for (int k = 0; k < 2; k++)
								{
									p[k] = new float[dim_output + 1];
									c[k] = new float[dim_output + 1];
									
									p[k][0] = inputs[i + k];
									c[k][0] = (k + 1) * inputs[i] / 3 + (2 - k) * inputs[j] / 3;
									for (int l = 1; l < p.Length; l++)
									{
										p[k][l] = outp[i + k][l - 1];
										c[k][l] = tang[i + k][l - 1];
									}
								}
								
								s = (time - inputs[i]) / (inputs[j] - inputs[i]);
								for (int k = 0; k < result.Length; k++)
								{
									result[k] = bezierInterpolate(s, p[0][k], c[0][k], c[1][k], p[1][k]);
								}
								return result;
							}
							else if (dim_tangents == dim_output + 1)
							{
								result = new float[dim_output];
								s = approximateCubicBezierParameter(time, inputs[i], tang[0][0], tang[1][0], inputs[j]);
								for (int k = 0; k < result.Length; k++)
								{
									result[k] = bezierInterpolate(s, outp[0][k], tang[0][k + 1], tang[1][k + 1], outp[1][k]);
								}
								return result;
							}
							else if (dim_tangents == dim_output * 2)
							{
								result = new float[dim_output];
								for (int k = 0; k < dim_output; k++)
								{
									s = approximateCubicBezierParameter(time, inputs[i], tang[0][2 * k], tang[0][2 * k], inputs[j]);
									result[k] = bezierInterpolate(s, outp[0][k], tang[0][2 * k + 1], tang[1][2 * k + 1], outp[1][k]);
								}
								return result;
							}
							else
							{
								throw new ColladaException("unknown tangent format in bezier animation curve!");
							}
						case EInterpolation.HERMITE:
						case EInterpolation.BSPLINE:
						case EInterpolation.CARDINAL:
							break;
					}

					throw new ColladaException("sampling failed!");
				}
				
				private int findKeyFrame(float time)
				{
					return findKeyFrame(time, 0, inputs.Length - 1);
				}
				
				private int findKeyFrame(float time, int l, int h)
				{
					if (l == h || l == h - 1)
						return l;
					
					int m = l + (h - l) / 2;
					if (time > inputs[m])
						return findKeyFrame(time, m, h);
					else
						return findKeyFrame(time, l, m);
				}
				
				//simply clamps a value between 0 .. 1
				private float clampToZeroOne(float value) {
				   if (value < .0f)
				      return .0f;
				   else if (value > 1.0f)
				      return 1.0f;
				   else
				      return value;
				}
				
				/**
				 * Returns the approximated parameter of a parametric curve for the value X
				 * @param atX At which value should the parameter be evaluated
				 * @param P0_X The first interpolation point of a curve segment
				 * @param C0_X The first control point of a curve segment
				 * @param C1_X The second control point of a curve segment
				 * @param P1_x The second interpolation point of a curve segment
				 * @return The parametric argument that is used to retrieve atX using the parametric function representation of this curve
				 */
				private float approximateCubicBezierParameter (
				         float atX, float P0_X, float C0_X, float C1_X, float P1_X ) {
				   
				   if (atX - P0_X < VERYSMALL)
				      return 0.0f;
				   
				   if (P1_X - atX < VERYSMALL) 
				      return 1.0f;
				   
				   long iterationStep = 0;
				   
				   float u = 0.0f; float v = 1.0f;
				   
				   //iteratively apply subdivision to approach value atX
				   while (iterationStep < MAXIMUM_ITERATIONS) {
				      
				      // de Casteljau Subdivision.
				      float a = (P0_X + C0_X)*0.5f;
				      float b = (C0_X + C1_X)*0.5f;
				      float c = (C1_X + P1_X)*0.5f;
				      float d = (a + b)*0.5f;
				      float e = (b + c)*0.5f;
				      float f = (d + e)*0.5f; //this one is on the curve!
				      
				      //The curve point is close enough to our wanted atX
				      if (Math.Abs(f - atX) < APPROXIMATION_EPSILON) {
				         return clampToZeroOne((u + v)*0.5f);
				      }
				      
				      //dichotomy
				      if (f < atX) {
				         P0_X = f;
				         C0_X = e;
				         C1_X = c;
				         u = (u + v)*0.5f;
				      } else {
				         C0_X = a; 
				         C1_X = d; 
				         P1_X = f; 
				         v = (u + v)*0.5f;
				      }
				      
				      iterationStep++;
				   }
				   return clampToZeroOne((u + v)*0.5f);
				   
				}
				
				private float bezierInterpolate(float s, float p0, float c0, float c1, float p1)
				{
					return (float) (Math.Pow(1 - s, 3) * p0 + 3 * Math.Pow(1 - s, 2) * s * c0 + 3 * (1 - s) * Math.Pow(s, 2) * c1 + Math.Pow(s, 3) * p1);
				}

			}
			
			private Dictionary<string, Sampler> samplers;
			private List<Channel> channels;
			private List<Animation> children;
			
        	public Animation(Document doc, Model model, Document.Animation animation)
        	{
        		children = new List<Animation>();
        		samplers = new Dictionary<string, Sampler>();
        		channels = new List<Channel>();
        		
        		if (animation.children != null)
        			foreach (Document.Animation childAnim in animation.children)
        				children.Add(new Animation(doc, model, childAnim));
        		
        		if (animation.channels != null && animation.samplers != null)
        		{
	        		foreach (Document.Sampler sampler in animation.samplers)
	        			samplers.Add(sampler.id, new Sampler(doc, sampler));
	        		foreach (Document.Channel channel in animation.channels)
	        		{
	        			try
	        			{
	        				channels.Add(new Channel(doc, model, channel, samplers));
	        			}
	        			catch (ColladaException e)
	        			{
	        				COLLADAUtil.Log(e);
	        			}
	        		}
        		}
        	}
        	
        	public void apply(float time)
        	{
        		foreach (Animation anim in children)
        		{
        			anim.apply(time);
        		}
        		
        		foreach (Channel channel in channels)
        		{
        			channel.Apply(time);
        		}
        	}
        } // Animation
        
        // Summary:
        //     Provides properties for modifying a traditional fixed-function–style material,
        //     as supported by most 3D modeling packages.
        [Serializable]
        public class BasicMaterial
        {
            // Summary:
            //     Initializes a new instance of Microsoft.Xna.Framework.Content.Pipeline.Graphics.BasicMaterial.
            public BasicMaterial() {}

            // Summary:
            //     Gets or sets the alpha property.
            //
            // Returns:
            //     Current alpha value or the value to be set.
            public float? Alpha { get { return alpha; } set {alpha = value; } }
            private float? alpha;
            //
            // Summary:
            //     Gets or sets the diffuse color property.
            //
            // Returns:
            //     Current diffuse color value or the value to be set.
            public Vector3? DiffuseColor { get { return diffuseColor; } set { diffuseColor = value; } }
            private Vector3? diffuseColor;
            //
            // Summary:
            //     Gets or sets the emissive color property.
            //
            // Returns:
            //     Current diffuse color value or the value to be set.
            public Vector3? EmissiveColor { get { return emissiveColor; } set { emissiveColor = value; } }
            private Vector3? emissiveColor;
            //
            // Summary:
            //     Gets or sets the specular color property.
            //
            // Returns:
            //     Current specular color value or the value to be set.
            public Vector3? SpecularColor { get { return specularColor; } set { specularColor = value; } }
            private Vector3? specularColor;
            //
            // Summary:
            //     Gets or sets the specular power property.
            //
            // Returns:
            //     Current specular power value or the value to be set.
            public float? SpecularPower { get { return specularPower; } set { specularPower = value; } }
            private float? specularPower;
            //
            // Summary:
            //     Gets or sets the diffuse texture property.
            //
            // Returns:
            //     Current diffuse texture value or the value to be set.
            public string Texture { get {return texture;} set {texture = value;} }
            private string texture;
            //
            // Summary:
            //     Gets or sets the vertex color property.
            //
            // Returns:
            //     Current vertex color or the value to be set.
            public bool? VertexColorEnabled { get {return vertexColorEnabled;} set {vertexColorEnabled = value;} }
            private bool? vertexColorEnabled;
        } // BasicMaterial

        // Summary:
        //    Additional indirection to enable mesh instancing
        [Serializable]
        public class InstanceMesh
        {   			
            //
            // Summary:
            //     Gets or set the instanced Mesh.
            //
            // Returns:
            //     The instanced Model.ModelMesh mesh.
            public CMesh Mesh { get { return mesh; } set { mesh = value; } }
            private CMesh mesh;

            //
            // Summary:
            //     Gets or set the bone for this instance
            //
            // Returns:
            //     The Model.ModelBone that this instance is attached to.
            public Bone ParentBone { get { return parentBone; } set { ParentBone = value; } }
            private Bone parentBone;

            //
            // Summary:
            //     Gets the materialbinding information for the mesh parts of this mesh
            //
            // Returns:
            //     the material binding dictionary .
            public Dictionary<string, string> MaterialBinding { get { return materialBinding; } }
            private Dictionary<string, string> materialBinding;

            //
            // Summary:
            //     Gets all the BaseEffects used by all the MeshParts in this instance
            //
            // Returns:
            //     a BasicEffect list.
            public List<BaseEffect> Effects { get { return effects; } set { effects = value; } }
            private List<BaseEffect> effects;

            //
            // Summary:
            //     Gets all the BaseEffects used by all the MeshParts in this instance
            //
            // Returns:
            //     a list of Effects which elemts matches with the MeshParts in the Mesh.
            public List<BaseEffect> PartEffects { get { return partEffects; } set { partEffects = value; } }
            private List<BaseEffect> partEffects;

            private InstanceMesh(){}
            // Summary:
            //     Create a new Instance of a Mesh
            // Parameters:
            //   mesh:
            //     the ModelMesh to instance
            //   parentBone:
            //     the bone that will give this instance its position
            //   materialBinding
            //     the binding between the material name and the name in the mesh parts
            // 
            // Returns:
            //     A new instance of mesh
            public InstanceMesh(Model.CMesh mesh, Model.Bone parentBone, Dictionary<string, string> materialBinding)
            {
                this.mesh = mesh;
                this.parentBone = parentBone;
                this.materialBinding = materialBinding;
            }
        }
        
        public class SkinnedInstanceMesh : InstanceMesh
        {
        
        	private Document.Node skeletonRootNode;
        	private Bone skeletonRootBone;
        	private CSkinnedMesh skinnedMesh;
        	private List<Bone> bones;
        	private List<Matrix> invBindMatrixList;
        	private Matrix bindShapeMatrix;
        	
        	public SkinnedInstanceMesh(CSkinnedMesh mesh, Bone parentBone, 
        	                           Dictionary<string, string> materialBinding, Document.Node skeletonRootNode)
        		: base(mesh, parentBone, materialBinding)
        	{
        		this.skinnedMesh = mesh;
        		this.skeletonRootNode = skeletonRootNode;
        		//this.skeletonRootBone = skeletonRootBone;
        		
        		bindShapeMatrix = new Matrix();
        		bindShapeMatrix.M11 = mesh.Skin.bindShapeMatrix[0, 0];
        		bindShapeMatrix.M12 = mesh.Skin.bindShapeMatrix[1, 0];
        		bindShapeMatrix.M13 = mesh.Skin.bindShapeMatrix[2, 0];
        		bindShapeMatrix.M14 = mesh.Skin.bindShapeMatrix[3, 0];
        		bindShapeMatrix.M21 = mesh.Skin.bindShapeMatrix[0, 1];
        		bindShapeMatrix.M22 = mesh.Skin.bindShapeMatrix[1, 1];
        		bindShapeMatrix.M23 = mesh.Skin.bindShapeMatrix[2, 1];
        		bindShapeMatrix.M24 = mesh.Skin.bindShapeMatrix[3, 1];
        		bindShapeMatrix.M31 = mesh.Skin.bindShapeMatrix[0, 2];
        		bindShapeMatrix.M32 = mesh.Skin.bindShapeMatrix[1, 2];
        		bindShapeMatrix.M33 = mesh.Skin.bindShapeMatrix[2, 2];
        		bindShapeMatrix.M34 = mesh.Skin.bindShapeMatrix[3, 2];
        		bindShapeMatrix.M41 = mesh.Skin.bindShapeMatrix[0, 3];
        		bindShapeMatrix.M42 = mesh.Skin.bindShapeMatrix[1, 3];
        		bindShapeMatrix.M43 = mesh.Skin.bindShapeMatrix[2, 3];
        		bindShapeMatrix.M44 = mesh.Skin.bindShapeMatrix[3, 3];
        	}
        	
        	public List<Matrix> GetSkinningMatrices() {
        		LoadBones();
        		
        		List<Matrix> boneMatrixList = new List<Matrix>();
        		for (int i = 0; i < invBindMatrixList.Count; i++)
        		{
        			boneMatrixList.Add(bindShapeMatrix * invBindMatrixList[i] * bones[i].AbsoluteTransformMatrix);
					//boneMatrixList.Add(bindShapeMatrix);
        		}
        		
        		return boneMatrixList;
        	}
        	
        	public List<Vector3> GetSkeletonVertices()
        	{
        		LoadBones();
        		
        		List<Vector3> boneVertices = new List<Vector3>();
        		for (int i = 0; i < bones.Count; i++)
        		{
        			Vector3 s, t1, t2;
        			Quaternion q;
        			(bones[i].AbsoluteTransformMatrix).Decompose(out s, out q, out t1);
        			
        			foreach (Bone b in bones[i].Children)
        			{
        				(b.AbsoluteTransformMatrix).Decompose(out s, out q, out t2);
        				boneVertices.Add(t1);
        				boneVertices.Add(t2);
        			}
        		}
        		
        		return boneVertices;
        	}
        	
        	private void LoadBones() {
        		if (bones == null)
        		{
        			bones = new List<Bone>();
        			invBindMatrixList = new List<Matrix>();
        			Document.Source source;
        			
        			((CSkinnedMesh)Mesh).Model.BonesTable.TryGetValue(skeletonRootNode.id, out skeletonRootBone);
        			
        			Model model = skinnedMesh.Model;
        			foreach (Document.Input input in skinnedMesh.Skin.joint.inputs)
	        		{
	        			switch (input.semantic)
	        			{
	        				case "JOINT":
	        					source = (Document.Source) input.source;
	        					Document.Array<string> keys = (Document.Array<string>) source.array;
	        					switch (source.arrayType)
	        					{
	        						case "IDREF_array":
	        							// target nodes are addressed absolute
	        							for (int i = 0; i < keys.Count; i++)
	        							{
	        								bones.Add(model.BonesTable[keys[i]]);
	        							}
	        							break;
	        						case "Name_array":
	        							// target nodes are relative to skeletonRootNode
	        							for (int i = 0; i < keys.Count; i++)
	        							{
	        								bones.Add(model.BonesTable[COLLADAUtil.GetNodeByName(skeletonRootNode, keys[i]).id]);
	        							}
	        							break;
	       							default:
			        					COLLADAUtil.Log(COLLADALogType.Warning, "Unkown array type '" + source.arrayType + "' in skinning controller.");
			        					break;
	        					}
	        					break;
	        				case "INV_BIND_MATRIX":
	        					source = (Document.Source) input.source;
	        					switch (source.arrayType)
	        					{
	        						case "float_array":
	        							Document.Array<float> arr = (Document.Array<float>) source.array;
	        							switch (source.accessor.stride)
	        							{
	        								case 16:
	        									for (int i = 0; i < arr.Count; i += 16)
	        									{
	        										Matrix m = new Matrix();
	        										m.M11 = arr[i];
	        										m.M21 = arr[i + 1];
	        										m.M31 = arr[i + 2];
	        										m.M41 = arr[i + 3];
	        										m.M12 = arr[i + 4];
	        										m.M22 = arr[i + 5];
	        										m.M32 = arr[i + 6];
	        										m.M42 = arr[i + 7];
	        										m.M13 = arr[i + 8];
	        										m.M23 = arr[i + 9];
	        										m.M33 = arr[i + 10];
	        										m.M43 = arr[i + 11];
	        										m.M14 = arr[i + 12];
	        										m.M24 = arr[i + 13];
	        										m.M34 = arr[i + 14];
	        										m.M44 = arr[i + 15];
	        										invBindMatrixList.Add(m);
	        									}
	        									break;
	        								default:
	        									throw new Exception("TODO: support more INV_BIND_MATRIX types!");
	        							}
	        							break;
	        						default:
	        							throw new Exception("TODO: support more INV_BIND_MATRIX types!");
	        					}
	        					break;
	        				default:
	        					COLLADAUtil.Log(COLLADALogType.Warning, "Unkown semantic '" + input.semantic + "' in skinning controller.");
	        					break;
	        			}
	        		}
        			
        			if (invBindMatrixList.Count != bones.Count)
        			{
        				throw new Exception("Count of inverse bind matrices must be equal to count of joints in skinning controller!");
        			}
        		}
        	}
        	
        	public override string ToString()
			{
				return skinnedMesh.ToString();
			}
        	
        }
        
		#endregion
        
		#region members
		// angle conversion
		public const float DegToRad = 0.0174532925199432957692f;

        // The COLLADA Document used to create this model.
        public Document Doc { get { return doc; } }
        private Document doc;
        
        // A collection of CMesh objects used by this model.    
        private Dictionary<string, CMesh> meshes;    
        
        // A collection of CSkinnedMesh objects used by this model.    
        private Dictionary<string, CSkinnedMesh> skinnedMeshes;    
        
		// A collection of Bone objects used by this model.
        private Dictionary<string, Bone> bones;
        public ICollection<Bone> Bones { get { return bones.Values; } }
        
        // A binding of nodeID to Bone
        public Dictionary<string, Bone> BonesTable { get { return bones; } }


        // The root bone for this model.
        private Bone root;
        public Bone Root { get { return root; } }

        // Gets or sets an object identifying this model.
        private object tag;
        public object Tag { get { return tag; } set {tag=value;} }

        // A collection of BasicMaterial associated with this model.
        private Dictionary<string, BasicMaterial> basicMaterials;
        public ICollection<BasicMaterial> BasicMaterials { get { return basicMaterials.Values; } }
        // A binding of materialID to BasicMaterial
        public Dictionary<string, BasicMaterial> BasicMaterialsBinding { get { return basicMaterials; } }
        
        private Dictionary<string, Animation> animations;
        public Dictionary<string, Animation> AnimationsBinding { get { return animations; } }

        // A collection of InstanceMeshes
        public List<InstanceMesh> InstanceMeshes { get { return instanceMeshes; } }
        private List<InstanceMesh> instanceMeshes;
        
        // private temporaries
        private Dictionary<string, Dictionary<string, uint>> textureBindings;
        #endregion
        
        #region constructor
        // Summary:
        //     Initializes a new instance of Model.
        // This will work only if the COLLADA geometry has been processed for vertex array
        // Parameters:
        //   _document:
        //      The collada document
        public Model(Document _doc)
        {
            // internals
            doc = _doc;
            
            // create meshes
            meshes = new Dictionary<string, CMesh>();
            foreach (Document.Geometry geo in doc.geometries)
            {
            	// only load valid meshes
            	if (geo.mesh != null && geo.mesh.primitives != null && geo.mesh.primitives.Count > 0)
            		meshes[geo.id] = new CMesh(this, geo);
            	else
            		COLLADAUtil.Log(COLLADALogType.Warning, "Geometry '" + geo.id + "' seems invalid ... skipping.");
            }
            
            // skinned meshes will be created in ReadNode method, because the only way to tell if a mesh
            // needs skinning is by reading the scene graph of collada.
            // since skinned meshes need additional data in vertex and index buffers this is a litte dirty.
            skinnedMeshes = new Dictionary<string, CSkinnedMesh>();

            // create materials
            // Need a list of meshPart per material...., and most probably a link from the meshPart to the mesh
            basicMaterials = new Dictionary<string, BasicMaterial>();
            textureBindings = new Dictionary<string, Dictionary<string, uint>>();
            foreach (Document.Material material in doc.materials)
                basicMaterials[material.id] = createBasicMaterial(this, material);
            
            // get the model from the instanced visual scene
            Document.VisualScene scene = doc.dic[doc.instanceVisualScene.url.Fragment] as Document.VisualScene;
            if (scene == null) throw new Exception("NO VISUAL SCENE (MODEL) IN DOCUMENT");

            // recursive call to load scene
            bones = new Dictionary<string, Bone>();
            instanceMeshes = new List<InstanceMesh>();
            root = new Model.Bone(this,null,scene.id);

            foreach (Document.Node node in scene.nodes)
                root.Children.Add(ReadNode(root, node));
            
            // create animations
			animations = new Dictionary<string, Animation>();
            foreach (Document.Animation animation in doc.animations)
            {
            	try 
            	{
            		animations.Add(animation.id, new Animation(doc, this, animation));
            	}
            	catch (ColladaException e)
            	{
            		COLLADAUtil.Log(e);
            	}
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// <param name="node">The "<node>" element to be converted.</param>
        /// </summary>
        private Bone ReadNode(Bone parent, Document.Node node)
        {
        	Bone bone;
        	//if (BonesTable.TryGetValue(node.id, out bone))
        	//	return bone;
        	
        	bone = new Bone(this, parent, node.id);

            if (node.instances != null)
                foreach (Document.Instance instance in node.instances)
                {
                    if (instance is Document.InstanceGeometry)
                    {
                        // resolve bindings
                        Dictionary<string, string> materialBinding = ResolveMaterialBinding((Document.InstanceWithMaterialBind)instance);

                        Document.Geometry geo = (Document.Geometry)doc.dic[instance.url.Fragment];
                        CMesh modelMesh;
                        if (meshes.TryGetValue(geo.id, out modelMesh))
                        {
                        	InstanceMesh instanceMesh = new InstanceMesh(modelMesh,
	                                                                 bone,
	                                                                 materialBinding);
                        	instanceMeshes.Add(instanceMesh);
                        }
                        else
                        	COLLADAUtil.Log(COLLADALogType.Error, "mesh with id " + geo.id + " not found in internal mesh list");
                    }
                    else if (instance is Document.InstanceCamera)
                    {
                        // TODO: camera
                    }
                    else if (instance is Document.InstanceLight)
                    {
                        // TODO: light
                    }
                    else if (instance is Document.InstanceController)
                    {
                    	Document.InstanceController instanceController = (Document.InstanceController) instance;
                        Document.ISkinOrMorph skinOrMorph = ((Document.Controller)doc.dic[instanceController.url.Fragment]).controller;
                        if (skinOrMorph is Document.Skin)
                        {
                        	// resolve bindings
                            Dictionary<string, string> materialBinding = ResolveMaterialBinding((Document.InstanceWithMaterialBind)instance);
                        	Document.Skin skin = (Document.Skin) skinOrMorph;
                            Document.Geometry geo = ((Document.Geometry)doc.dic[skin.source.Fragment]);
                            
                            // make sure mesh is in loaded mesh list
                            CMesh mesh;
	                        if (meshes.TryGetValue(geo.id, out mesh))
	                        {
	                        	// mesh was loaded once successfully, try to look it up in the list of skinned
		                        // meshes (skinned meshes have different vertex declarations)...
	                            CSkinnedMesh skinnedMesh;
		                        if (!skinnedMeshes.TryGetValue(geo.id, out skinnedMesh))
		                        {
		                        	// mesh wasn't found, create it once
		                        	skinnedMesh = new CSkinnedMesh(this, geo, skin);
		                        	skinnedMeshes.Add(geo.id, skinnedMesh);
		                        }
		                        
		                        Document.Node skeletonRootNode = (Document.Node) doc.dic[instanceController.skeleton[0].Fragment];
		                        SkinnedInstanceMesh skinnedInstanceMesh = new SkinnedInstanceMesh(
		                                                                            skinnedMesh,
		                                                                            bone,
		                                                                            materialBinding,
		                                                                            skeletonRootNode);
	                            instanceMeshes.Add(skinnedInstanceMesh);
	                        }
	                        else
                        	COLLADAUtil.Log(COLLADALogType.Error, "mesh with id " + geo.id + " not found in internal mesh list");
                            
                        }
                        else if (skinOrMorph is Document.Morph)
                        {
                            // TODO: morph
                        }
                        else
                            throw new Exception("Unknowned type of controller:" + skinOrMorph.GetType().ToString());
                    }
                    else if (instance is Document.InstanceNode)
                    {
                        Document.Node instanceNode = ((Document.Node)doc.dic[instance.url.Fragment]);
                        bone = ReadNode(parent,node);

                    }
                    else
                        throw new Exception("Unkowned type of INode in scene :" + instance.GetType().ToString());
                }

            // read transforms

            if (node.transforms != null)
                foreach (Document.TransformNode transformNode in node.transforms)
                {

                    if (transformNode is Document.Translate)
                    	bone.Transforms.Add(transformNode.sid, new TranslateTransform(new Vector3(transformNode[0], transformNode[1], transformNode[2])));
                    else if (transformNode is Document.Rotate)
                    	bone.Transforms.Add(transformNode.sid, new RotateTransform(new Vector3(transformNode[0], transformNode[1], transformNode[2]), transformNode[3]));
                    else if (transformNode is Document.Lookat)
                    	bone.Transforms.Add(transformNode.sid, new Transform(Matrix.LookAtLH(new Vector3(transformNode[0], transformNode[1], transformNode[2]), new Vector3(transformNode[3], transformNode[4], transformNode[5]), new Vector3(transformNode[6], transformNode[7], transformNode[8]))));
                    else if (transformNode is Document.Matrix)
                    {
                    	Matrix m = Matrix.Identity;
                    	m.M11 = transformNode[0];
                    	m.M21 = transformNode[01];
                    	m.M31 = transformNode[02];
                    	m.M41 = transformNode[03];
                    	m.M12 = transformNode[04];
                    	m.M22 = transformNode[05];
                    	m.M32 = transformNode[06];
                    	m.M42 = transformNode[07];
                        m.M13 = transformNode[08];
                        m.M23 = transformNode[09];
                        m.M33 = transformNode[10];
                        m.M43 = transformNode[11];
                        m.M14 = transformNode[12];
                        m.M24 = transformNode[13];
                        m.M34 = transformNode[14];
                        m.M44 = transformNode[15];
                        bone.Transforms.Add(transformNode.sid, new Transform(m));
                    }
                    else if (transformNode is Document.Scale)
                    	bone.Transforms.Add(transformNode.sid, new ScaleTransform(new Vector3(transformNode[0], transformNode[1], transformNode[2])));
                    else if (transformNode is Document.Skew)
                    {
                        // Convert Skew to a matrix
                        float angle = transformNode[0] * (float) DegToRad;
                        Vector3 a = new Vector3(transformNode[1], transformNode[2], transformNode[3]);
                        Vector3 b = new Vector3(transformNode[4], transformNode[5], transformNode[6]);
                        Vector3 n2 = Vector3.Normalize(b);
                        Vector3 a1 = n2 * Vector3.Dot(a, n2);
                        Vector3 a2 = a - a1;
                        Vector3 n1 = Vector3.Normalize(a2);
                        float an1 = Vector3.Dot(a, n1);
                        float an2 = Vector3.Dot(a, n2);
                        double rx = an1 * Math.Cos(angle) - an2 * Math.Sin(angle);
                        double ry = an1 * Math.Sin(angle) + an2 * Math.Cos(angle);
                        float alpha = 0.0f;
                        Matrix m = Matrix.Identity;

                        if (rx <= 0.0) throw new Exception("Skew: angle too large");
                        if (an1 != 0) alpha = (float)(ry / rx - an2 / an1);

                        m.M11 = n1.X * n2.X * alpha + 1.0f;
                        m.M12 = n1.Y * n2.X * alpha;
                        m.M13 = n1.Z * n2.X * alpha;

                        m.M21 = n1.X * n2.Y * alpha;
                        m.M22 = n1.Y * n2.Y * alpha + 1.0f;
                        m.M23 = n1.Z * n2.Y * alpha;

                        m.M31 = n1.X * n2.Z * alpha;
                        m.M32 = n1.Y * n2.Z * alpha;
                        m.M33 = n1.Z * n2.Z * alpha + 1.0f;

                        bone.Transforms.Add(transformNode.sid, new Transform(m));
                    }
                }

            if (node.children != null)
                foreach (Document.Node child in node.children)
                {
                	bone.Children.Add(ReadNode(bone,child));
                }

            return bone;

        } // ReadNode()
     
        /// <summary>
        /// Resolve the material binding  for the given "<instance_material>".
        /// <param name="instance">The "<instance_material>" element we need to resolve the binding for.</param>
        /// <param name="context"> The current context for the COLLADA Processor</param>
        /// </summary>
        private Dictionary<string, string> ResolveMaterialBinding(Document.InstanceWithMaterialBind instance)
        {
        	// MaterialBinding contains the material_id bind to each symbol in the <mesh>
            Dictionary<string, string> materialBinding = new Dictionary<string, string>();
            // TextureCoordinateBinding contains XNA mesh channel number for a given texcoord set#
            Dictionary<uint, uint> textureCoordinateBinding = new Dictionary<uint, uint>();
            
        	if (instance.bindMaterial == null) return materialBinding; // TODO: log this
            
            foreach (KeyValuePair<string, Document.InstanceMaterial> de in instance.bindMaterial.instanceMaterials)
            {
                // material binding materialBinding[geometry_stmbol] = material
                materialBinding[((Document.InstanceMaterial)de.Value).symbol] = de.Key;
                if (de.Value.bindVertexInputs != null) // textureset binding 
                {
                    foreach (Document.InstanceMaterial.BindVertexInput bindVertexInput in de.Value.bindVertexInputs)
                    {
                        if (textureBindings[(string)de.Key].ContainsKey(bindVertexInput.semantic))
                        {
                            uint tmp = textureBindings[(string)de.Key][bindVertexInput.semantic];
                            uint tmp2 = bindVertexInput.inputSet;
                            textureCoordinateBinding[tmp2] = tmp;
                        }
                        else
                			COLLADAUtil.Log(COLLADALogType.Warning, "Can't find vertex input binding '" + bindVertexInput.semantic +"' for '" + de.Key + "'.");
                    }
                }
                else if (((Document.InstanceMaterial)de.Value).binds != null)
                {
                    foreach (Document.InstanceMaterial.Bind bind in ((Document.InstanceMaterial)de.Value).binds)
                    {
                		string key = (string)de.Key;
                		if (textureBindings.ContainsKey(key))
                		{
                			if (textureBindings[key].ContainsKey(bind.semantic))
                			{
		                        uint tmp = textureBindings[key][bind.semantic];
		                        // assuming only one texture coordinate set
		                        textureCoordinateBinding[0] = tmp;
                			}
                		}
                		else
                			COLLADAUtil.Log(COLLADALogType.Warning, "Can't find texture binding '" + key + "'.");
                    }
                }
            }
            
            return materialBinding;
        }

        /// <summary>
        /// Create a BasicMaterial from a "<material>".
        /// <param name="material">The "<material>" element to be converted.</param>
        /// <param name="context"> The current context for the COLLADA Processor</param>
        /// </summary>
        public BasicMaterial createBasicMaterial(Model model, Document.Material material)
        {
            Document doc = model.Doc;
            uint textureChannel = 0;

            Dictionary<string, uint> textureBinding = new Dictionary<string, uint>();
            textureBindings[material.id] = textureBinding;
            BasicMaterial materialContent = new BasicMaterial();
            Document.Effect effect = (Document.Effect)doc.dic[material.instanceEffect.Fragment];

            if (effect == null) throw new Exception("cannot find effect#" + material.instanceEffect.Fragment);
            // search common profile with correct asset....
            Document.ProfileCOMMON profile;
            foreach (Document.IProfile tmpProfile in effect.profiles)
            {
                if (tmpProfile is Document.ProfileCOMMON)
                {
                    profile = (Document.ProfileCOMMON)tmpProfile;
                    goto Found;
                }
            }
            throw new Exception("Could not find profile_COMMON in effect" + effect.ToString());
        Found:
            // read params
           // Dictionary<string, string> samplerBind = new Dictionary<string, string>();
           // Dictionary<string, string> imageBind = new Dictionary<string, string>();

            // Read Technique
            Document.SimpleShader shader = ((Document.ProfileCOMMON)profile).technique.shader;

            // BasicShader only accept texture for the diffuse channel
            if (shader.diffuse is Document.Texture)
            {
                string sampler = ((Document.Texture)shader.diffuse).texture;
                string surface;
                string image;
                if (profile.newParams.ContainsKey(sampler))
                {
                    surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
                    image = ((Document.Surface)profile.newParams[surface].param).initFrom;
                }
                else
                {
                    image = sampler;
                    // TODO: emit serious warning - invalid content
                }
                // now find image
                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
                // here associate 1 texture binding per texture in material
                textureBinding[((Document.Texture)shader.diffuse).texcoord] = textureChannel++;
                materialContent.Texture = imagePath;
            }
            else if (shader.diffuse is Document.Color)
            {
                Document.Color color = (Document.Color)shader.diffuse;
                // TODO: manage color[3] in transparency
                materialContent.DiffuseColor = new Vector3(color[0], color[1], color[2]);
            }
            if (shader.ambient is Document.Texture)
            {
                // Basic Material does not accept texture on ambient channel
                /*
                string sampler = ((Document.Texture)shader.ambient).texture;
                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
                // now find image
                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
                // here associate 1 texture binding per texture in material
                textureBinding[((Document.Texture)shader.ambient).texcoord] = textureChannel++;
                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
                */

            }
            else if (shader.ambient is Document.Color)
            {
                // XNA BasicMaterial has no ambient 
            }
            if (shader.emission is Document.Texture)
            {
                // XNA BasicMaterial does not accept texture for emmision
                /*
                string sampler = ((Document.Texture)shader.emission).texture;
                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
                // now find image
                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
                // here associate 1 texture binding per texture in material
                textureBinding[((Document.Texture)shader.emission).texcoord] = textureChannel++;
                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
                 */
            }
            else if (shader.emission is Document.Color)
            {
                Document.Color color = (Document.Color)shader.emission;
                materialContent.EmissiveColor = new Vector3(color[0], color[1], color[2]);
            }
            if (shader.specular is Document.Texture)
            {
                // XNA BasicMaterial does not accept texture for specular
                /*
                string sampler = ((Document.Texture)shader.specular).texture;
                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
                // now find image
                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
                // here associate 1 texture binding per texture in material
                textureBinding[((Document.Texture)shader.specular).texcoord] = textureChannel++;
                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
                 */
            }
            else if (shader.specular is Document.Color)
            {
                Document.Color color = (Document.Color)shader.specular;
                materialContent.SpecularColor = new Vector3(color[0], color[1], color[2]);
                if (shader.shininess is Document.Float)
                    materialContent.SpecularPower = ((Document.Float)shader.shininess).theFloat;
            }
            if (shader.transparency is Document.Texture)
            {
                // XNA Basic Shader does not accept a texture for the transparency channel
                /*
                string sampler = ((Document.Texture)shader.transparency).texture;
                string surface = ((Document.Sampler2D)profile.newParams[sampler].param).source;
                string image = ((Document.Surface)profile.newParams[surface].param).initFrom;
                // now find image
                string imagePath = ((Document.Image)doc.dic[image]).init_from.Uri.LocalPath;
                // here associate 1 texture binding per texture in material
                textureBinding[((Document.Texture)shader.transparency).texcoord] = textureChannel++;
                materialContent.Texture = new ExternalReference<TextureContent>(imagePath);
                 */
            }
            else if (shader.transparency is Document.Float)
            {
                materialContent.Alpha = ((Document.Float)shader.transparency).theFloat;
            }

            return materialContent;
        }
        
    	public Mesh createUnion3D9Mesh(Device graphicsDevice, List<InstanceMesh> instanceMeshes, bool applyTranforms)
       	{
    		List<Mesh> meshes = new List<Mesh>();
    		
			int attribId = 0;
			int i = 0;
			foreach (Model.InstanceMesh instanceMesh in instanceMeshes)
    		{
				if (instanceMesh.Mesh.Primitives.Count > 0)
				{
					meshes.Add(instanceMesh.Mesh.Create3D9Mesh(graphicsDevice, ref attribId));
					attribId++;
					i++;
				}
    		}
        	
			Mesh mesh;
			if (applyTranforms)
			{
				List<Matrix> geometryTransforms = getTransformsOfUnionMesh(instanceMeshes);
				mesh = Mesh.Concatenate(graphicsDevice, meshes.ToArray(), MeshFlags.Use32Bit | MeshFlags.Managed, geometryTransforms.ToArray(), null, null);
			}
			else
				mesh = Mesh.Concatenate(graphicsDevice, meshes.ToArray(), MeshFlags.Use32Bit | MeshFlags.Managed);
			
			foreach (Mesh m in meshes)
				m.Dispose();
			
        	mesh.OptimizeInPlace(MeshOptimizeFlags.AttributeSort);

        	return mesh;
        }
    	
    	public List<Matrix> getTransformsOfUnionMesh(List<InstanceMesh> instanceMeshes)
    	{
    		List<Matrix> geometryTransforms = new List<Matrix>();
    		Matrix[] transforms = new Matrix[this.Bones.Count];
			CopyAbsoluteBoneTransformsTo(transforms);
			
			foreach (Model.InstanceMesh instanceMesh in instanceMeshes)
			{
				for (int i = 0; i < instanceMesh.Mesh.Primitives.Count; i++)
				{
					geometryTransforms.Add(transforms[instanceMesh.ParentBone.Index]);
				}
			}
			
			return geometryTransforms;
    	}
    	
    	public void applyAnimations(float time)
    	{
    		foreach (Animation animation in AnimationsBinding.Values)
    		{
    			animation.apply(time);
    		}
    	}
    	
        /// <summary>
        /// Copies an absolute transform of each bone in the model into a given array.
        /// </summary>
        /// <param name="destinationBoneTransforms">The array to receive bone transforms.</param>
        public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
        {
            // traverse tree from root, and fill in the Matrix[] result
            AbsoluteBoneTraversal(root, Matrix.Identity, destinationBoneTransforms);
        }
        
        private void AbsoluteBoneTraversal(Model.Bone bone, Matrix transform, Matrix[] destination)
        {
            transform = bone.TransformMatrix * transform;
            destination[bone.Index] = transform;
            foreach (Model.Bone child in bone.Children)
                AbsoluteBoneTraversal(child, transform, destination);
        }
        
        public Matrix GetAbsoluteTransformMatrix(InstanceMesh instanceMesh, float time)
        {
        	return GetAbsoluteTransformMatrix(instanceMesh.ParentBone, time);
        }
        
        public Matrix GetAbsoluteTransformMatrix(Bone bone, float time)
        {
        	foreach (Transform t in bone.Transforms.Values)
        		t.ApplyAnimations(time);
        	
        	if (bone.Parent == null)
        		return bone.TransformMatrix;
        	return bone.TransformMatrix * GetAbsoluteTransformMatrix(bone.Parent, time);
        }
        
        
    	#endregion
    } // Model

}
