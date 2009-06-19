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

using ColladaSlimDX.ColladaDocument;
#endregion

namespace ColladaSlimDX.Utils
{

    public class COLLADAUtil
    {
        /// <summary>
        /// Helper function, returns the p[] value for the given index
        /// </summary>
        /// <param name="input">The "<input>" element we need the index of.</param>
        /// <param name="primitive">The "<primitive>" element the "<input>" is from.</param>
        /// <param name="index"> The index for which we need the p[] value.</param>
        static public int GetPValue(Document.Input input, Document.Primitive primitive, int index)
        {
            int stride = primitive.stride;
            int offset = input.offset;
            return primitive.p[index * stride + offset];
        }
        /// <summary>
        /// Helper function, returns the element of a source for the given index
        /// </summary>
        /// <param name="doc">The COLLADA document</param>
        /// <param name="input">The "<input>" element we need the index of.</param>
        /// <param name="index"> The index for which we need the value.</param>
        public static float[] GetSourceElement(Document doc, Document.Input input, int index)
        {
            Document.Source src = (Document.Source)input.source;

            // resolve array
            // Note: this will work only if the array is in the current document...
            //       TODO: create a resolver funtion rather than access to doc.dic directly...
            //             enable loading array from binary raw file as well

            object array = doc.dic[src.accessor.source.Fragment];

            if (array is Document.Array<float>)
            {
                Document.Array<float> farray = (Document.Array<float>)(array);
                float[] returnValue = new float[src.accessor.stride];
                for (int i = 0; i < returnValue.Length; i++)
                	returnValue[i] = farray[src.accessor[i, index]];
                return returnValue;
            }
            else
                throw new Exception("Unsupported array type");
            // Note: Rarelly int_array could be used for geometry values
        }
        /// <summary>
        /// Helper function, returns the "<input>" that has the POSITION semantic
        /// </summary>
        /// <param name="mesh">The "<mesh>" element we need the POSITION from.</param>
        static public Document.Input GetPositionInput(Document.Mesh mesh)
        {
            int i;
            for (i = 0; i < mesh.vertices.inputs.Count; i++)
            {
                if (mesh.vertices.inputs[i].semantic == "POSITION")
                    return mesh.vertices.inputs[i];
            }
            throw new Exception("No POSITION in vertex input");
        }
        /// <summary>
        /// Helper function. Returns all the inputs of the primitive. 
        /// Resolve the 'VERTEX' indirection case.
        /// <param name="doc">The COLLADA document</param>
        /// <param name="primitive"> The "<primitive>" we need the inputs from.</param>
        /// </summary>
        static public List<Document.Input> GetAllInputs(Document.Primitive primitive)
        {
            List<Document.Input> inputs = new List<Document.Input>();

            // 1- get all the regular inputs
            foreach (Document.Input input in primitive.Inputs)
            {
                if (input.semantic == "VERTEX") 
                {
                	// 2- get all the indirect inputs
	                foreach (Document.Input input2 in ((Document.Vertices)input.source).inputs)
	                    inputs.Add(new Document.Input(input2.doc, input.offset, input2.semantic, input2.set, ((Document.Source)input2.source).id));
                }
                else
                    inputs.Add(input);
            }
            
            return inputs;
        }
        
        /// <summary>
        /// Helper function. Returns inputs with specified semantic for specified primitive.
        /// Resolves the 'VERTEX' indirection case.
        /// </summary>
        /// <param name="primitive">The "<primitive>" we need the inputs from.</param>
        /// <param name="semantic">The semantic the inputs should have.</param>
        public static List<Document.Input> GetInput(Document.Primitive primitive, string semantic)
        {
        	List<Document.Input> resultList = new List<Document.Input>();
        	List<Document.Input> inputs = GetAllInputs(primitive);
        	foreach (Document.Input input in inputs)
        	{
        		if (input.semantic == semantic)
        			resultList.Add(input);
        	}
        	return resultList;
        }
        
        public static string getTargetNodeId(Document doc, string targetAddress)
        {
			string[] addressParts = targetAddress.Split('/');
			if (addressParts.Length == 0)
				throw new ColladaException("invalid target address: " + targetAddress);
			if (addressParts[0] == ".")
				throw new ColladaException("can't handle relative target address:" + targetAddress);
			if (!doc.dic.ContainsKey(addressParts[0]))
				throw new ColladaException("can't find node " + addressParts[0]);
			
			Document.Node targetNode = doc.dic[addressParts[0]] as Document.Node;
			for (int i = 1; i < addressParts.Length - 1; i++)
			{
				bool found = false;
				foreach (Document.Node node in targetNode.children)
				{
					if (node.sid == addressParts[i])
					{
						targetNode = node;
						found = true;
						break;
					}
				}
				if (!found)
					throw new ColladaException("invalid target address: " + targetAddress);
			}
			return targetNode.id;
        }
        
        public static Document.Node GetNodeByName(Document.Node root, string sid)
        {
        	if (root.sid == sid)
        		return root;
        	
        	if (root.children != null)
        	{
	        	Document.Node node;
	        	foreach (Document.Node child in root.children)
	        	{
	        		node = GetNodeByName(child, sid);
	        		if (node != null)
	        			return node;
	        	}
        	}
        	
        	return null;
        }
        
		public class SimpleLogger : ICOLLADALogger
		{
			public void Log(COLLADALogType LogType, string Message)
			{
				switch (LogType) 
				{
					case COLLADALogType.Debug: System.Diagnostics.Debug.WriteLine("    " + Message); break;
					case COLLADALogType.Message: System.Diagnostics.Debug.WriteLine(" -  " + Message); break;
					case COLLADALogType.Warning: System.Diagnostics.Debug.WriteLine(" *  " + Message); break;
					case COLLADALogType.Error: System.Diagnostics.Debug.WriteLine(" ERR  " + Message); break;
				}
			}
		}
        
        private static ICOLLADALogger FLogger;
		public static ICOLLADALogger Logger { set { FLogger = value; } }
		
		public static void Log(COLLADALogType type, string msg)
		{
			if (FLogger == null) {
				FLogger = new SimpleLogger();
			}
			FLogger.Log(type, msg);
		}
		
		public static void Log(Exception e)
		{
			Log(COLLADALogType.Error, e.Message);
			Log(COLLADALogType.Debug, e.StackTrace);
		}
		
		public static void Log(ColladaException e)
		{
			Log(COLLADALogType.Warning, e.Message);
			Log(COLLADALogType.Debug, e.StackTrace);
		}
		
		public static void Log(string msg)
		{
			Log(COLLADALogType.Debug, msg);
		}
        
    }
    
    public enum COLLADALogType {Debug, Message, Warning, Error};
    
	public interface ICOLLADALogger
	{
		void Log(COLLADALogType type, string msg);
	}
	
		/// <summary>
	/// Base class for collada specific exceptions
	/// </summary>
	public class ColladaException : Exception
	{
		public ColladaException(String message)
			: base(message)
		{
		}
	}
	
	/// <summary>
	/// Exception is thrown if element doesn't have a unique id
	/// </summary>
	public class NonUniqueIDException : ColladaException
	{
		public NonUniqueIDException(String message)
			: base(message)
		{
		}
	}
	
	/// <summary>
	/// Exception is thrown if a required child element doesn't exist
	/// </summary>
	public class MissingRequiredElementException : ColladaException
	{
		public MissingRequiredElementException(String message)
			: base(message)
		{
		}
	}
	
}
