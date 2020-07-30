using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    /// <summary>
    /// 基于特定关键字加载当前上下文中的所有程序集，
    /// 查找所有类型。
    /// </summary>
    internal class AppDomainTypeFinder
    {
        #region Fields

        private bool ignoreReflectionErrors = true;
        private readonly object _syncObj = new object();
        private Type[] _types;

        #endregion

        #region Properties

        /// <summary>当前AppDomain,应用进程</summary>
        public virtual AppDomain App
        {
            get { return AppDomain.CurrentDomain; }
        }

        /// <summary>
        /// 是否自动加载运行时上下文的程序集，如果为false，只加载配置过的程序集。
        /// </summary>
        public bool LoadAppDomainAssemblies { get; set; } = true;

        /// <summary>
        /// 配置需要加载的程序集。
        /// </summary>
        public IList<string> AssemblyNames { get; set; } = new List<string>();

        /// <summary>忽略的DLL</summary>
        public string AssemblySkipLoadingPattern { get; set; } = "^System|^mscorlib|^Microsoft|^AjaxControlToolkit|^Antlr3|^Autofac|^AutoMapper|^Castle|^ComponentArt|^CppCodeProvider|^DotNetOpenAuth|^EntityFramework|^EPPlus|^FluentValidation|^ImageResizer|^itextsharp|^log4net|^MaxMind|^MbUnit|^MiniProfiler|^Mono.Math|^MvcContrib|^Newtonsoft|^NHibernate|^nunit|^Org.Mentalis|^PerlRegex|^QuickGraph|^Recaptcha|^Remotion|^RestSharp|^Rhino|^Telerik|^Iesi|^TestDriven|^TestFu|^UserAgentStringLibrary|^VJSharpCodeProvider|^WebActivator|^WebDev|^WebGrease|^Devexpress";

        public string AssemblyRestrictToLoadingPattern { get; set; } = ".*";

        #endregion

        #region Methods

        public IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true)
        {
            var result = new List<Type>();

            var types = GetAllTypes();

            foreach (var t in types)
            {
                if (assignTypeFrom.IsAssignableFrom(t) || (assignTypeFrom.IsGenericTypeDefinition && DoesTypeImplementOpenGeneric(t, assignTypeFrom)))
                {
                    if (!t.IsInterface)
                    {
                        if (onlyConcreteClasses)
                        {
                            if (t.IsClass && !t.IsAbstract)
                            {
                                result.Add(t);
                            }
                        }
                        else
                        {
                            result.Add(t);
                        }
                    }
                }
            }

            return result;
        }

        public IEnumerable<Type> FindClassesOfType<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), assemblies, onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            var result = new List<Type>();
            try
            {
                foreach (var a in assemblies)
                {
                    Type[] types = null;
                    try
                    {
                        types = a.GetTypes();
                    }
                    catch
                    {
                        //Entity Framework 6 doesn't allow getting types (throws an exception)
                        if (!ignoreReflectionErrors)
                        {
                            throw;
                        }
                    }
                    if (types != null)
                    {
                        foreach (var t in types)
                        {
                            if (assignTypeFrom.IsAssignableFrom(t) || (assignTypeFrom.IsGenericTypeDefinition && DoesTypeImplementOpenGeneric(t, assignTypeFrom)))
                            {
                                if (!t.IsInterface)
                                {
                                    if (onlyConcreteClasses)
                                    {
                                        if (t.IsClass && !t.IsAbstract)
                                        {
                                            result.Add(t);
                                        }
                                    }
                                    else
                                    {
                                        result.Add(t);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var msg = string.Empty;
                foreach (var e in ex.LoaderExceptions)
                    msg += e.Message + Environment.NewLine;

                var fail = new Exception(msg, ex);
                Debug.WriteLine(fail.Message, fail);

                throw fail;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IList<Assembly> GetAssemblies()
        {
            //记录已经添加过的程序集，避免重复添加。
            var addedAssemblyNames = new List<string>();
            var assemblies = new List<Assembly>();

            if (LoadAppDomainAssemblies)
                AddAssembliesInAppDomain(addedAssemblyNames, assemblies);
            AddConfiguredAssemblies(addedAssemblyNames, assemblies);

            return assemblies;
        }


        public Type[] Find(Func<Type, bool> predicate)
        {
            var types = GetAllTypes().Where(predicate).ToArray();
            return types;
        }

        public Type[] FindAll()
        {
            return GetAllTypes().ToArray();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// 遍历AppDomain中的程序集，符合条件就添加到列表。
        /// </summary>
        /// <param name="addedAssemblyNames">已添加的程序集名称</param>
        /// <param name="assemblies">需要添加到的程序集结果列表</param>
        private void AddAssembliesInAppDomain(List<string> addedAssemblyNames, List<Assembly> assemblies)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (Matches(assembly.FullName))
                {
                    if (!addedAssemblyNames.Contains(assembly.FullName))
                    {
                        assemblies.Add(assembly);
                        addedAssemblyNames.Add(assembly.FullName);
                    }
                }
            }
        }

        /// <summary>
        /// 将属性AssemblyNames中显式配置的程序集加载到列表中。
        /// </summary>
        /// <param name="addedAssemblyNames"></param>
        /// <param name="assemblies"></param>
        protected virtual void AddConfiguredAssemblies(List<string> addedAssemblyNames, List<Assembly> assemblies)
        {
            foreach (string assemblyName in AssemblyNames)
            {
                Assembly assembly = Assembly.Load(assemblyName);
                if (!addedAssemblyNames.Contains(assembly.FullName))
                {
                    assemblies.Add(assembly);
                    addedAssemblyNames.Add(assembly.FullName);
                }
            }
        }

        /// <summary>
        /// 判断是否符合添加条件
        /// </summary>
        /// <param name="assemblyFullName"></param>
        /// <returns></returns>
        public virtual bool Matches(string assemblyFullName)
        {
            return !Matches(assemblyFullName, AssemblySkipLoadingPattern)
                   && Matches(assemblyFullName, AssemblyRestrictToLoadingPattern);
        }
        protected bool Matches(string assemblyFullName, string pattern)
        {
            return Regex.IsMatch(assemblyFullName, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// 加载目录下符合条件的程序集
        /// </summary>
        internal List<Assembly> LoadMatchingAssemblies(string directoryPath)
        {
            var loadedAssemblyNames = new List<Assembly>();
            foreach (Assembly a in GetAssemblies())
            {
                loadedAssemblyNames.Add(a);
            }

            //foreach (string dllPath in Directory.GetFiles(directoryPath, "*.dll"))
            //{
            //    try
            //    {
            //        var an = Assembly.LoadFile(dllPath);
            //        if (Matches(an.FullName) && !loadedAssemblyNames.Contains(an))
            //        {
            //            loadedAssemblyNames.Add(an);
            //        }
            //    }
            //    catch (BadImageFormatException ex)
            //    {
            //        Trace.TraceError(ex.ToString());
            //    }
            //}
            return loadedAssemblyNames;
        }

        /// <summary>
        /// 判断类型是否是泛型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="openGeneric"></param>
        /// <returns></returns>
        protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
        {
            try
            {
                var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
                foreach (var implementedInterface in type.FindInterfaces((objType, objCriteria) => true, null))
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    var isMatch = genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition());
                    return isMatch;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected virtual Type[] GetAllTypes()
        {
            if (_types == null)
            {
                lock (_syncObj)
                {
                    if (_types == null)
                    {
                        _types = CreateTypeList().ToArray();
                    }
                }
            }

            return _types;
        }

        protected virtual List<Type> CreateTypeList()
        {
            var allTypes = new List<Type>();
            var assemblies = GetAssemblies().Distinct();
            foreach (var assembly in assemblies)
            {
                try
                {
                    Type[] typesInThisAssembly;

                    try
                    {
                        typesInThisAssembly = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        typesInThisAssembly = ex.Types;
                    }

                    if (typesInThisAssembly.Length == 0)
                    {
                        continue;
                    }

                    allTypes.AddRange(typesInThisAssembly.Where(type => type != null));
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            return allTypes;
        }


        #endregion

        public virtual void Clear()
        {

        }
    }
}
