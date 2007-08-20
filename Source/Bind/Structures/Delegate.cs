#region --- License ---
/* Copyright (c) 2006, 2007 Stefanos Apostolopoulos
 * See license.txt for license info
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace Bind.Structures
{
    /// <summary>
    /// Represents an opengl function.
    /// The return value, function name, function parameters and opengl version can be retrieved or set.
    /// </summary>
    public class Delegate
    {
        internal static DelegateCollection Delegates;

        private static bool delegatesLoaded;
        
        #region internal static void Initialize(string glSpec, string glSpecExt)
        
        internal static void Initialize(string glSpec, string glSpecExt)
        {
            if (!delegatesLoaded)
            {
                using (StreamReader sr = Utilities.OpenSpecFile(Settings.InputPath, glSpec))
                {
                    Delegates = Bind.MainClass.Generator.ReadDelegates(sr);
                }

                if (!String.IsNullOrEmpty(glSpecExt))
                {
                    using (StreamReader sr = Utilities.OpenSpecFile(Settings.InputPath, glSpecExt))
                    {
                        foreach (Delegate d in Bind.MainClass.Generator.ReadDelegates(sr).Values)
                        {
                            Utilities.Merge(Delegates, d);
                        }
                    }
                }
                delegatesLoaded = true;
            }
        }

        #endregion
        
        #region --- Constructors ---

        public Delegate()
        {
            Parameters = new ParameterCollection();
        }

        public Delegate(Delegate d)
        {
            this.Category = !String.IsNullOrEmpty(d.Category) ? new string(d.Category.ToCharArray()) : "";
            //this.Extension = !String.IsNullOrEmpty(d.Extension) ? new string(d.Extension.ToCharArray()) : "";
            this.Name = new string(d.Name.ToCharArray());
            //this.NeedsWrapper = d.NeedsWrapper;
            this.Parameters = new ParameterCollection(d.Parameters);
            this.ReturnType = new Type(d.ReturnType);
            this.Version = !String.IsNullOrEmpty(d.Version) ? new string(d.Version.ToCharArray()) : "";
            //this.Unsafe = d.Unsafe;
        }

        #endregion

        #region --- Properties ---

        #region public bool CLSCompliant

        /// <summary>
        ///  Gets the CLSCompliant property. True if the delegate is not CLSCompliant.
        /// </summary>
        public bool CLSCompliant
        {
            get
            {
                if (Unsafe)
                    return false;

                if (!ReturnType.CLSCompliant)
                    return false;

                foreach (Parameter p in Parameters)
                {
                    if (!p.CLSCompliant)
                        return false;
                }
                return true;
            }
        }

        #endregion

        #region public string Category

        private string _category;

        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }

        #endregion

        #region public bool NeedsWrapper

        /// <summary>
        /// Indicates whether this function needs to be wrapped with a Marshaling function.
        /// This flag is set if a function contains an Array parameter, or returns
        /// an Array or string.
        /// </summary>
        public bool NeedsWrapper
        {
            //get { return _needs_wrapper; }
            //set { _needs_wrapper = value; }

            get
            {
                // TODO: Add special cases for (Get)ShaderSource.

                if (ReturnType.WrapperType != WrapperTypes.None)
                    return true;

                foreach (Parameter p in Parameters)
                {
                    if (p.WrapperType != WrapperTypes.None)
                        return true;
                }

                return false;
            }
        }

        #endregion

        #region public virtual bool Unsafe

        /// <summary>
        /// True if the delegate must be declared as 'unsafe'.
        /// </summary>
        public virtual bool Unsafe
        {
            //get { return @unsafe; }
            //set { @unsafe = value; }
            get
            {
                if (ReturnType.Pointer)
                    return true;

                foreach (Parameter p in Parameters)
                {
                    if (p.Pointer)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion

        #region public Parameter ReturnType

        Type _return_type = new Type();
        /// <summary>
        /// Gets or sets the return value of the opengl function.
        /// </summary>
        public Type ReturnType
        {
            get { return _return_type; }
            set
            {
                _return_type = Type.Translate(value);
            }
        }

        #endregion

        #region public virtual string Name

        string _name;
        /// <summary>
        /// Gets or sets the name of the opengl function.
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    _name = value.Trim();
                }
            }
        }

        #endregion

        #region public ParameterCollection Parameters

        ParameterCollection _parameters;

        public ParameterCollection Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        #endregion

        #region public string Version

        string _version;

        /// <summary>
        /// Defines the opengl version that introduced this function.
        /// </summary>
        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        #endregion

        #region public bool Extension

        string _extension;

        public string Extension
        {
            //get { return _extension; }
            //set { _extension = value; }
            get
            {
                if (!String.IsNullOrEmpty(Name))
                {
                    _extension = Utilities.GetGL2Extension(Name);
                    return String.IsNullOrEmpty(_extension) ? "Core" : _extension;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #endregion

        #region --- Strings ---

        #region public string CallString()

        public string CallString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Settings.DelegatesClass);
            sb.Append(".");
            sb.Append(Settings.FunctionPrefix);
            sb.Append(Name);
            sb.Append(Parameters.CallString(Settings.Compatibility == Settings.Legacy.Tao));

            return sb.ToString();
        }

        #endregion

        #region public string DeclarationString()

        public string DeclarationString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Unsafe ? "unsafe " : "");
            sb.Append(ReturnType);
            sb.Append(" ");
            sb.Append(Name);
            sb.Append(Parameters.ToString());

            return sb.ToString();
        }

        #endregion

        #region override public string ToString()

        /// <summary>
        /// Gets the string representing the full function declaration without decorations
        /// (ie "void glClearColor(float red, float green, float blue, float alpha)"
        /// </summary>
        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Unsafe ? "unsafe " : "");
            sb.Append("delegate ");
            sb.Append(ReturnType);
            sb.Append(" ");
            sb.Append(Name);
            sb.Append(Parameters.ToString());

            return sb.ToString();
        }

        #endregion

        public Delegate GetCLSCompliantDelegate()
        {
            Delegate f = new Delegate(this);

            for (int i = 0; i < f.Parameters.Count; i++)
            {
                f.Parameters[i].CurrentType = f.Parameters[i].GetCLSCompliantType();
            }

            f.ReturnType.CurrentType = f.ReturnType.GetCLSCompliantType();

            return f;
        }

        #endregion

        #region --- Wrapper Creation ---

        #region public IEnumerable<Function> CreateWrappers()

        public void CreateWrappers()
        {
            if (this.Name.Contains("GenBuffers"))
            {
            }

            List<Function> wrappers = new List<Function>();
            if (!NeedsWrapper)
            {
                // No special wrapper needed - just call this delegate:
                Function f = new Function(this);

                if (f.ReturnType.CurrentType.ToLower().Contains("void"))
                    f.Body.Add(String.Format("{0};", f.CallString()));
                else
                    f.Body.Add(String.Format("return {0};", f.CallString()));

                wrappers.Add(f);
            }
            else
            {
                Function f = WrapReturnType();

                WrapParameters(new Function((Function)f ?? this), wrappers);
            }

            // If the function is not CLS-compliant (e.g. it contains unsigned parameters)
            // we need to create a CLS-Compliant overload. However, we should only do this
            // iff the opengl function does not contain unsigned/signed overloads itself
            // to avoid redefinitions.
            foreach (Function f in wrappers)
            {
                Bind.Structures.Function.Wrappers.AddChecked(f);
                //Bind.Structures.Function.Wrappers.Add(f);

                if (!f.CLSCompliant)
                {
                    Function cls = new Function(f);

                    cls.Body.Clear();
                    if (!cls.NeedsWrapper)
                    {
                        cls.Body.Add((f.ReturnType.CurrentType != "void" ? "return " + this.CallString() : this.CallString()) + ";");
                    }
                    else
                    {
                        cls.Body.AddRange(this.CreateBody(cls, true));
                    }

                    bool somethingChanged = false;
                    for (int i = 0; i < f.Parameters.Count; i++)
                    {
                        cls.Parameters[i].CurrentType = cls.Parameters[i].GetCLSCompliantType();
                        if (cls.Parameters[i].CurrentType != f.Parameters[i].CurrentType)
                            somethingChanged = true;
                    }

                    if (somethingChanged)
                        Bind.Structures.Function.Wrappers.AddChecked(cls);
                }
            }
        }

        #endregion

        #region protected Function WrapReturnType()

        protected Function WrapReturnType()
        {
            // We have to add wrappers for all possible WrapperTypes.
            Function f;

            // First, check if the return type needs wrapping:
            switch (this.ReturnType.WrapperType)
            {
                // If the function returns a string (glGetString) we must manually marshal it
                // using Marshal.PtrToStringXXX. Otherwise, the GC will try to free the memory
                // used by the string, resulting in corruption (the memory belongs to the
                // unmanaged boundary).
                case WrapperTypes.StringReturnType:
                    f = new Function(this);
                    f.ReturnType.CurrentType = "System.String";

                    f.Body.Add(
                        String.Format(
                            "return System.Runtime.InteropServices.Marshal.PtrToStringAnsi({0});",
                            this.CallString()
                        )
                    );

                    return f;         // Only occurs in glGetString, there's no need to check parameters.

                // If the function returns a void* (GenericReturnValue), we'll have to return an IntPtr.
                // The user will unfortunately need to marshal this IntPtr to a data type manually.
                case WrapperTypes.GenericReturnType:
                    ReturnType.CurrentType = "IntPtr";
                    ReturnType.Pointer = false;

                    break;

                case WrapperTypes.None:
                default:
                    // No return wrapper needed
                    break;
            }

            return null;
        }
        
        #endregion

        #region protected void WrapParameters(Function function, List<Function> wrappers)

        protected static int index = 0;

        /// <summary>
        /// This function needs some heavy refactoring. I'm ashamed I ever wrote it, but it works...
        /// What it does is this: it adds to the wrapper list all possible wrapper permutations
        /// for functions that have more than one IntPtr parameter. Example:
        /// "void Delegates.f(IntPtr p, IntPtr q)" where p and q are pointers to void arrays needs the following wrappers:
        /// "void f(IntPtr p, IntPtr q)"
        /// "void f(IntPtr p, object q)"
        /// "void f(object p, IntPtr q)"
        /// "void f(object p, object q)"
        /// </summary>
        protected void WrapParameters(Function function, List<Function> wrappers)
        {
            if (index == 0)
            {
                bool containsPointerParameters = false, containsReferenceParameters = false;
                // Check if there are any IntPtr parameters (we may have come here from a ReturnType wrapper
                // such as glGetString, which contains no IntPtr parameters)
                foreach (Parameter p in function.Parameters)
                {
                    if (p.Pointer)
                    {
                        containsPointerParameters = true;
                        break;
                    }
                    else if (p.Reference)
                    {
                        containsReferenceParameters = true;
                        break;
                    }
                }

                if (containsPointerParameters)
                {
                    wrappers.Add(DefaultWrapper(function));
                }
                else if (containsReferenceParameters)
                {
                }
                else
                {
                    if (function.Body.Count == 0)
                        wrappers.Add(DefaultWrapper(function));
                    else
                        wrappers.Add(function);
                    return;
                }
            }

            if (index >= 0 && index < function.Parameters.Count)
            {
                Function f;

                if (function.Parameters[index].WrapperType == WrapperTypes.None)
                {
                    // No wrapper needed, visit the next parameter
                    ++index;
                    WrapParameters(function, wrappers);
                    --index;
                }
                else
                {
                    switch (function.Parameters[index].WrapperType)
                    {
                        case WrapperTypes.ArrayParameter:
                            // Recurse to the last parameter
                            ++index;
                            WrapParameters(function, wrappers);
                            --index;

                            if (function.Name == "UseFontOutlinesA")
                            {
                            }

                            // On stack rewind, create array wrappers
                            f = new Function(function);
                            f.Parameters[index].Reference = false;
                            f.Parameters[index].Array = 1;
                            f.Parameters[index].Pointer = false;
                            f.Body = CreateBody(f, false);
                            //f = ReferenceWrapper(new Function(function), index);
                            wrappers.Add(f);

                            // Recurse to the last parameter again, keeping the Array wrappers
                            ++index;
                            WrapParameters(f, wrappers);
                            --index;

                            f = new Function(function);
                            f.Parameters[index].Reference = true;
                            f.Parameters[index].Array = 0;
                            f.Parameters[index].Pointer = false;
                            f.Body = CreateBody(f, false);
                            //f = ReferenceWrapper(new Function(function), index);
                            wrappers.Add(f);

                            // Keeping the current Ref wrapper, visit all other parameters once more
                            ++index;
                            WrapParameters(f, wrappers);
                            --index;

                            break;

                        case WrapperTypes.GenericParameter:
                            // Recurse to the last parameter
                            ++index;
                            WrapParameters(function, wrappers);
                            --index;

                            // On stack rewind, create array wrappers
                            f = new Function(function);
                            f.Parameters[index].Reference = false;
                            f.Parameters[index].Array = 0;
                            f.Parameters[index].Pointer = false;
                            f.Parameters[index].CurrentType = "object";
                            f.Parameters[index].Flow = Parameter.FlowDirection.Undefined;

                            f.Body = CreateBody(f, false);
                            wrappers.Add(f);

                            // Keeping the current Object wrapper, visit all other parameters once more
                            ++index;
                            WrapParameters(f, wrappers);
                            --index;

                            break;

                        case WrapperTypes.ReferenceParameter:
                            // Recurse to the last parameter
                            ++index;
                            WrapParameters(function, wrappers);
                            --index;

                            // On stack rewind, create reference wrappers
                            f = new Function(function);
                            f.Parameters[index].Reference = true;
                            f.Parameters[index].Array = 0;
                            f.Parameters[index].Pointer = false;
                            f.Body = CreateBody(f, false);
                            //f = ReferenceWrapper(new Function(function), index);
                            wrappers.Add(f);

                            // Keeping the current Object wrapper, visit all other parameters once more
                            ++index;
                            WrapParameters(f, wrappers);
                            --index;

                            break;
                    }
                }
            }
        }

        #endregion

        #region protected Function DefaultWrapper(Function f)

        protected Function DefaultWrapper(Function f)
        {
            bool returns = f.ReturnType.CurrentType.ToLower().Contains("void") && !f.ReturnType.Pointer;
            string callString = String.Format(
                "{0} {1}{2}; {3}",
                Unsafe ? "unsafe {" : "",
                returns ? "" : "return ",
                this.CallString(),
                Unsafe ? "}" : "");

            f.Body.Add(callString);

            return f;
        }

        #endregion

        #region protected FunctionBody CreateBody(Function fun, bool wantCLSCompliance)

        static List<string> handle_statements = new List<string>();
        static List<string> fixed_statements = new List<string>();
        static List<string> assign_statements = new List<string>();
        static string function_call_statement;

        protected FunctionBody CreateBody(Function fun, bool wantCLSCompliance)
        {
            Function f = new Function(fun);

            f.Body.Clear();
            handle_statements.Clear();
            fixed_statements.Clear();
            assign_statements.Clear();

            if (f.Name == "LoadDisplayColorTableEXT")
            { 
            }

            // Obtain pointers by pinning the parameters
            int param = 0;
            foreach (Parameter p in f.Parameters)
            {
                if (p.NeedsPin)
                {
                    // Use GCHandle to obtain pointer to generic parameters and 'fixed' for arrays.
                    // This is because fixed can only take the address of fields, not managed objects.
                    if (p.WrapperType == WrapperTypes.GenericParameter)
                    {
                        handle_statements.Add(String.Format(
                            "{0} {1} = {0}.Alloc({2}, System.Runtime.InteropServices.GCHandleType.Pinned);",
                            "System.Runtime.InteropServices.GCHandle", p.Name + "_ptr", p.Name));

                        if (p.Flow == Parameter.FlowDirection.Out)
                        {
                            assign_statements.Add(String.Format(
                                "        {0} = ({1}){2}.Target;",
                                p.Name, p.CurrentType, p.Name + "_ptr"));
                        }

                        // Note! The following line modifies f.Parameters, *not* function.Parameters
                        p.Name = "(void*)" + p.Name + "_ptr.AddrOfPinnedObject()";
                    }
                    else if (p.WrapperType == WrapperTypes.PointerParameter ||
                        p.WrapperType == WrapperTypes.ArrayParameter ||
                        p.WrapperType == WrapperTypes.ReferenceParameter)
                    {
                        fixed_statements.Add(String.Format(
                            "fixed ({0}* {1} = {2})",
                            wantCLSCompliance && !p.CLSCompliant ? p.GetCLSCompliantType() : p.CurrentType,
                            p.Name + "_ptr",
                            p.Array > 0 ? p.Name : "&" + p.Name));

                        if (p.Flow == Parameter.FlowDirection.Out && p.Array == 0)  // Fixed Arrays of blittable types don't need explicit assignment.
                        {
                            assign_statements.Add(String.Format("        {0} = *{0}_ptr;", p.Name));
                        }

                        p.Name = p.Name + "_ptr";
                    }
                    else
                    {
                        throw new ApplicationException("Unknown parameter type");
                    }
                }
            }

            //if (!f.Unsafe && (fixed_statements.Count > 0 || fixed_statements.Count > 0))
            {
                f.Body.Add("unsafe");
                f.Body.Add("{");
                f.Body.Indent();
            }

            if (fixed_statements.Count > 0)
            {
                f.Body.AddRange(fixed_statements);
                f.Body.Add("{");
                f.Body.Indent();
            }

            if (handle_statements.Count > 0)
            {
                f.Body.AddRange(handle_statements);
                f.Body.Add("try");
                f.Body.Add("{");
                f.Body.Indent();
            }

            if (f.ReturnType.CurrentType.ToLower().Contains("void"))
            {
                f.Body.Add(String.Format("{0};", f.CallString()));
            }
            else
            {
                f.Body.Add(String.Format("{0} {1} = {2};", f.ReturnType.CurrentType, "retval", f.CallString()));
            }

            if (assign_statements.Count > 0)
            {
                f.Body.AddRange(assign_statements);
            }

            // Return:
            if (!f.ReturnType.CurrentType.ToLower().Contains("void"))
            {
                f.Body.Add("return retval;");
            }

            if (handle_statements.Count > 0)
            {
                f.Body.Unindent();
                f.Body.Add("}");
                f.Body.Add("finally");
                f.Body.Add("{");
                f.Body.Indent();
                // Free all allocated GCHandles
                foreach (Parameter p in this.Parameters)
                {
                    if (p.NeedsPin && p.WrapperType == WrapperTypes.GenericParameter)
                    {
                        f.Body.Add(String.Format("    {0}_ptr.Free();", p.Name));
                    }
                }
                f.Body.Unindent();
                f.Body.Add("}");
            }

            if (fixed_statements.Count > 0)
            {
                f.Body.Unindent();
                f.Body.Add("}");
            }

            //if (!f.Unsafe && (fixed_statements.Count > 0 || fixed_statements.Count > 0))
            {
                f.Body.Unindent();
                f.Body.Add("}");
            }

            return f.Body;
        }

        #endregion

        #endregion

        #region Translate

        /// <summary>
        /// Translates the opengl return type to the equivalent C# type.
        /// </summary>
        /// <param name="d">The opengl function to translate.</param>
        /// <remarks>
        /// First, we use the official typemap (gl.tm) to get the correct type.
        /// Then we override this, when it is:
        /// 1) A string (we have to use Marshal.PtrToStringAnsi, to avoid heap corruption)
        /// 2) An array (translates to IntPtr)
        /// 3) A generic object or void* (translates to IntPtr)
        /// 4) A GLenum (translates to int on Legacy.Tao or GL.Enums.GLenum otherwise).
        /// Return types must always be CLS-compliant, because .Net does not support overloading on return types.
        /// </remarks>
        protected virtual void TranslateReturnType()
        {
            if (Bind.Structures.Type.GLTypes.ContainsKey(ReturnType.CurrentType))
                ReturnType.CurrentType = Bind.Structures.Type.GLTypes[ReturnType.CurrentType];

            if (Bind.Structures.Type.CSTypes.ContainsKey(ReturnType.CurrentType))
                ReturnType.CurrentType = Bind.Structures.Type.CSTypes[ReturnType.CurrentType];

            if (ReturnType.CurrentType.ToLower().Contains("void") && ReturnType.Pointer)
            {
                ReturnType.WrapperType = WrapperTypes.GenericReturnType;
            }

            if (ReturnType.CurrentType.ToLower().Contains("string"))
            {
                ReturnType.CurrentType = "IntPtr";
                ReturnType.WrapperType = WrapperTypes.StringReturnType;
            }

            if (ReturnType.CurrentType.ToLower().Contains("object"))
            {
                ReturnType.CurrentType = "IntPtr";
                ReturnType.WrapperType |= WrapperTypes.GenericReturnType;
            }

            if (ReturnType.CurrentType.Contains("GLenum"))
            {
                if (Settings.Compatibility == Settings.Legacy.None)
                    ReturnType.CurrentType =
                        String.Format("{0}.{1}",
                            Settings.GLEnumsClass,
                            Settings.CompleteEnumName);
                else
                    ReturnType.CurrentType = "int";
            }

            if (ReturnType.CurrentType.ToLower().Contains("bool"))
            {
                // TODO: Is the translation to 'int' needed 100%? It breaks WGL.
                /*
                if (Settings.Compatibility == Settings.Legacy.Tao)
                {
                    ReturnType.CurrentType = "int";
                }
                else
                {
                }
                */

                //ReturnType.WrapperType = WrapperTypes.ReturnsBool;
            }

            ReturnType.CurrentType = ReturnType.GetCLSCompliantType();
        }

        protected virtual void TranslateParameters()
        {
            if (this.Name.Contains("SetLayerPaletteEntries"))
            {
            //	Console.WriteLine();
            }
            for (int i = 0; i < Parameters.Count; i++)
            {
                Parameters[i] = Parameter.Translate(Parameters[i], this.Category);

                // Special cases: glLineStipple and gl(Get)ShaderSource:
                // Check for LineStipple (should be unchecked)
                if (Parameters[i].CurrentType == "UInt16" && Name.Contains("LineStipple"))
                {
                    Parameters[i].WrapperType = WrapperTypes.UncheckedParameter;
                }

                if (Name.Contains("ShaderSource") && Parameters[i].CurrentType.ToLower().Contains("string"))
                {
                    // Special case: these functions take a string[]
                    //IsPointer = true;
                    Parameters[i].Array = 1;
                }
            }
        }

        internal void Translate()
        {
            TranslateReturnType();
            TranslateParameters();

            CreateWrappers();
        }

        #endregion
    }

    #region class DelegateCollection : Dictionary<string, Delegate>

    class DelegateCollection : Dictionary<string, Delegate>
    {
        public void Add(Delegate d)
        {
            if (!this.ContainsKey(d.Name))
            {
                this.Add(d.Name, d);
            }
            else
            {
                Trace.WriteLine(String.Format(
                    "Spec error: function {0} redefined, ignoring second definition.",
                    d.Name));
            }
        }
    }

    #endregion
}
