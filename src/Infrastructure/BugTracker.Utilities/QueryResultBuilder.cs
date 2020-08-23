/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Querying;

    //internal sealed class QueryResultBuilder<TResult> : IQueryResultBuilder<TResult>
    //    where TResult : class, IResult
    //{
    //    private readonly TResult instance;

    //    public QueryResultBuilder()
    //    {
    //        var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
    //        var assemblyBuilder =
    //            AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    //        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

    //        var objectType = typeof(TResult);
    //        string typeName;

    //        if (objectType.IsInterface)
    //            typeName = objectType.Name.Substring(1);
    //        else
    //            typeName = $"{objectType.Name}Overrided";

    //        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

    //        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

    //        if (objectType.IsInterface)
    //        {
    //            typeBuilder.AddInterfaceImplementation(objectType);

    //            foreach (var property in typeof(TResult).GetProperties())
    //            {
    //                var fieldBuilder = typeBuilder.DefineField(
    //                    $"_{property.Name}",
    //                    property.PropertyType,
    //                    FieldAttributes.Private);

    //                var propertyBuilder = typeBuilder.DefineProperty(
    //                    property.Name,
    //                    PropertyAttributes.HasDefault,
    //                    property.PropertyType,
    //                    null);

    //                var getSetAttr = MethodAttributes.Public
    //                                 | MethodAttributes.SpecialName
    //                                 | MethodAttributes.HideBySig
    //                                 | MethodAttributes.Virtual;

    //                var getAccessor = typeBuilder.DefineMethod(
    //                    $"get_{property.Name}",
    //                    getSetAttr,
    //                    property.PropertyType,
    //                    Type.EmptyTypes);

    //                var getIL = getAccessor.GetILGenerator();

    //                getIL.Emit(OpCodes.Ldarg_0);
    //                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
    //                getIL.Emit(OpCodes.Ret);

    //                propertyBuilder.SetGetMethod(getAccessor);

    //                var setAccessor = typeBuilder.DefineMethod(
    //                    $"set_{property.Name}",
    //                    getSetAttr,
    //                    null,
    //                    new[] {property.PropertyType});

    //                var setIL = setAccessor.GetILGenerator();

    //                setIL.Emit(OpCodes.Ldarg_0);
    //                setIL.Emit(OpCodes.Ldarg_1);
    //                setIL.Emit(OpCodes.Stfld, fieldBuilder);
    //                setIL.Emit(OpCodes.Ret);

    //                propertyBuilder.SetSetMethod(setAccessor);
    //            }

    //            foreach (var interf in typeof(TResult).GetInterfaces())
    //            {
    //                typeBuilder.AddInterfaceImplementation(interf);

    //                foreach (var property in interf.GetProperties())
    //                {
    //                    var fieldBuilder = typeBuilder.DefineField(
    //                        $"_{property.Name}",
    //                        property.PropertyType,
    //                        FieldAttributes.Private);

    //                    var propertyBuilder = typeBuilder.DefineProperty(
    //                        property.Name,
    //                        PropertyAttributes.HasDefault,
    //                        property.PropertyType,
    //                        null);

    //                    var getSetAttr = MethodAttributes.Public
    //                                     | MethodAttributes.SpecialName
    //                                     | MethodAttributes.HideBySig
    //                                     | MethodAttributes.Virtual;

    //                    var getAccessor = typeBuilder.DefineMethod(
    //                        $"get_{property.Name}",
    //                        getSetAttr,
    //                        property.PropertyType,
    //                        Type.EmptyTypes);

    //                    var getIL = getAccessor.GetILGenerator();

    //                    getIL.Emit(OpCodes.Ldarg_0);
    //                    getIL.Emit(OpCodes.Ldfld, fieldBuilder);
    //                    getIL.Emit(OpCodes.Ret);

    //                    propertyBuilder.SetGetMethod(getAccessor);

    //                    var setAccessor = typeBuilder.DefineMethod(
    //                        $"set_{property.Name}",
    //                        getSetAttr,
    //                        null,
    //                        new[] {property.PropertyType});

    //                    var setIL = setAccessor.GetILGenerator();

    //                    setIL.Emit(OpCodes.Ldarg_0);
    //                    setIL.Emit(OpCodes.Ldarg_1);
    //                    setIL.Emit(OpCodes.Stfld, fieldBuilder);
    //                    setIL.Emit(OpCodes.Ret);

    //                    propertyBuilder.SetSetMethod(setAccessor);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            typeBuilder.SetParent(objectType);
    //        }

    //        var type = typeBuilder.CreateType();

    //        this.instance = (TResult) Activator.CreateInstance(type);
    //    }

    //    public IQueryResultBuilder<TResult> WithValue<TValue>(Expression<Func<TResult, TValue>> expression,
    //        TValue value)
    //    {
    //        if (expression != null && expression.Body is MemberExpression memberExpression)
    //            if (memberExpression.Member is PropertyInfo property)
    //            {
    //                var instanceMemberExpression =
    //                    Expression.Property(Expression.Constant(this.instance), property.Name);
    //                var instanceProperty = (PropertyInfo) instanceMemberExpression.Member;

    //                instanceProperty.SetValue(this.instance, value, null);
    //            }

    //        return this;
    //    }

    //    public TResult Build()
    //    {
    //        return this.instance;
    //    }
    //}

    //internal sealed class QueryResultBuilder2<TResult> : IQueryResultBuilder2<TResult>
    //    where TResult : class
    //{
    //    private readonly TResult instance;

    //    public QueryResultBuilder2()
    //    {
    //        var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
    //        var assemblyBuilder =
    //            AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    //        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

    //        var objectType = typeof(TResult);
    //        string typeName;

    //        if (objectType.IsInterface)
    //            typeName = objectType.Name.Substring(1);
    //        else
    //            typeName = $"{objectType.Name}Overrided";

    //        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

    //        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

    //        if (objectType.IsInterface)
    //        {
    //            typeBuilder.AddInterfaceImplementation(objectType);

    //            foreach (var property in typeof(TResult).GetProperties())
    //            {
    //                var fieldBuilder = typeBuilder.DefineField(
    //                    $"_{property.Name}",
    //                    property.PropertyType,
    //                    FieldAttributes.Private);

    //                var propertyBuilder = typeBuilder.DefineProperty(
    //                    property.Name,
    //                    PropertyAttributes.HasDefault,
    //                    property.PropertyType,
    //                    null);

    //                var getSetAttr = MethodAttributes.Public
    //                                 | MethodAttributes.SpecialName
    //                                 | MethodAttributes.HideBySig
    //                                 | MethodAttributes.Virtual;

    //                var getAccessor = typeBuilder.DefineMethod(
    //                    $"get_{property.Name}",
    //                    getSetAttr,
    //                    property.PropertyType,
    //                    Type.EmptyTypes);

    //                var getIL = getAccessor.GetILGenerator();

    //                getIL.Emit(OpCodes.Ldarg_0);
    //                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
    //                getIL.Emit(OpCodes.Ret);

    //                propertyBuilder.SetGetMethod(getAccessor);

    //                var setAccessor = typeBuilder.DefineMethod(
    //                    $"set_{property.Name}",
    //                    getSetAttr,
    //                    null,
    //                    new[] {property.PropertyType});

    //                var setIL = setAccessor.GetILGenerator();

    //                setIL.Emit(OpCodes.Ldarg_0);
    //                setIL.Emit(OpCodes.Ldarg_1);
    //                setIL.Emit(OpCodes.Stfld, fieldBuilder);
    //                setIL.Emit(OpCodes.Ret);

    //                propertyBuilder.SetSetMethod(setAccessor);
    //            }

    //            foreach (var interf in typeof(TResult).GetInterfaces())
    //            {
    //                typeBuilder.AddInterfaceImplementation(interf);

    //                foreach (var property in interf.GetProperties())
    //                {
    //                    var fieldBuilder = typeBuilder.DefineField(
    //                        $"_{property.Name}",
    //                        property.PropertyType,
    //                        FieldAttributes.Private);

    //                    var propertyBuilder = typeBuilder.DefineProperty(
    //                        property.Name,
    //                        PropertyAttributes.HasDefault,
    //                        property.PropertyType,
    //                        null);

    //                    var getSetAttr = MethodAttributes.Public
    //                                     | MethodAttributes.SpecialName
    //                                     | MethodAttributes.HideBySig
    //                                     | MethodAttributes.Virtual;

    //                    var getAccessor = typeBuilder.DefineMethod(
    //                        $"get_{property.Name}",
    //                        getSetAttr,
    //                        property.PropertyType,
    //                        Type.EmptyTypes);

    //                    var getIL = getAccessor.GetILGenerator();

    //                    getIL.Emit(OpCodes.Ldarg_0);
    //                    getIL.Emit(OpCodes.Ldfld, fieldBuilder);
    //                    getIL.Emit(OpCodes.Ret);

    //                    propertyBuilder.SetGetMethod(getAccessor);

    //                    var setAccessor = typeBuilder.DefineMethod(
    //                        $"set_{property.Name}",
    //                        getSetAttr,
    //                        null,
    //                        new[] {property.PropertyType});

    //                    var setIL = setAccessor.GetILGenerator();

    //                    setIL.Emit(OpCodes.Ldarg_0);
    //                    setIL.Emit(OpCodes.Ldarg_1);
    //                    setIL.Emit(OpCodes.Stfld, fieldBuilder);
    //                    setIL.Emit(OpCodes.Ret);

    //                    propertyBuilder.SetSetMethod(setAccessor);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            typeBuilder.SetParent(objectType);
    //        }

    //        var type = typeBuilder.CreateType();

    //        this.instance = (TResult) Activator.CreateInstance(type);
    //    }

    //    public IQueryResultBuilder2<TResult> WithValue<TValue>(Expression<Func<TResult, TValue>> expression,
    //        TValue value)
    //    {
    //        if (expression != null && expression.Body is MemberExpression memberExpression)
    //            if (memberExpression.Member is PropertyInfo property)
    //            {
    //                var instanceMemberExpression =
    //                    Expression.Property(Expression.Constant(this.instance), property.Name);
    //                var instanceProperty = (PropertyInfo) instanceMemberExpression.Member;

    //                instanceProperty.SetValue(this.instance, value, null);
    //            }

    //        return this;
    //    }

    //    public TResult Build()
    //    {
    //        return this.instance;
    //    }
    //}
}