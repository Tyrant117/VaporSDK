using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEngine;
using Vapor.Blueprints;
using Vapor.Inspector;

namespace VaporEditor.Blueprints
{
    public static class BlueprintScriptWriter
    {
        private static BlockSyntax s_CurrentBody;
        public static readonly Dictionary<string, BlueprintSyntaxNodeBase> NodeMap = new();

        public static void WriteScript(BlueprintGraphSo graphSo, string filePath)
        {
            var baseType = Type.GetType(graphSo.AssemblyQualifiedTypeName);
            if (baseType == null)
            {
                Debug.LogError($"Can't find graph type {graphSo.AssemblyQualifiedTypeName}");
                return;
            }

            WriteEmptyClass(baseType.Namespace, graphSo.name, baseType.Name, out var namespaceSyntax, out var classSyntax);

            foreach (var field in graphSo.DesignGraph.Variables)
            {
                var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(GetTypeSyntax(field.Type)) // field.Key = type
                            .AddVariables(SyntaxFactory.VariableDeclarator(field.Name))) // field.Value = name
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)); // Make it public

                classSyntax = classSyntax.AddMembers(fieldDeclaration);
            }

            foreach (var method in graphSo.DesignGraph.Methods)
            {
                NodeMap.Clear();
                EntrySyntaxNode entry = null;
                foreach (var n in method.Nodes)
                {
                    var syntaxNode = BlueprintSyntaxNodeBase.ConvertToSyntaxNode(n);
                    if (syntaxNode is EntrySyntaxNode esn)
                    {
                        entry = esn;
                    }

                    NodeMap.Add(n.Guid, syntaxNode);
                }

                if (entry == null)
                {
                    Debug.LogError($"Can't find entry for {method.MethodName} in {graphSo.name}");
                    continue;
                }
                
                if (method.IsOverride)
                {
                    TypeSyntax returnTypeSyntax = GetTypeSyntax(method.MethodInfo.ReturnType);
                    var body = SyntaxFactory.Block();

                    // if (method.MethodInfo.ReturnType != typeof(void))
                    // {
                    //     // Determine the default value based on the return type
                    //     ExpressionSyntax defaultValue = GetDefaultValueSyntax(method.MethodInfo.ReturnType);
                    //
                    //     // Create a variable declaration: <ReturnType> __returnValue = <defaultValue>;
                    //     var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    //         SyntaxFactory.VariableDeclaration(GetTypeSyntax(method.MethodInfo.ReturnType))
                    //             .AddVariables(SyntaxFactory.VariableDeclarator("__returnValue")
                    //                 .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign default value
                    //     );
                    //     
                    //     body = body.AddStatements(variableDeclaration);
                    // }
                    
                    body = entry.AddStatementAndContinue(body);
                    
                    var methodDeclaration = SyntaxFactory.MethodDeclaration(
                            returnTypeSyntax, // Corrected return type
                            SyntaxFactory.Identifier(method.MethodInfo.Name)) // Method name
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)) // Make it public
                        .AddParameterListParameters(method.MethodInfo.GetParameters().Select(param =>
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name))
                                    .WithType(GetTypeSyntax(param.ParameterType)) // Use the refined type syntax
                        ).ToArray())
                        .WithBody(body); // Blueprint Body

                    classSyntax = classSyntax.AddMembers(methodDeclaration);
                }
                else
                {
                    var methodNameSyntax = SyntaxFactory.Identifier(method.MethodName);
                    var parameters = CreateMethodParameters(method.InputArguments);
                    var returnType = CreateMethodReturnType(method.OutputArguments);
                    var methodDeclaration = CreateMethodDeclaration(methodNameSyntax, parameters, returnType);
                    var body = SyntaxFactory.Block();
                    body = entry.AddStatementAndContinue(body);
                    
                    methodDeclaration = methodDeclaration.AddBodyStatements(body.Statements.ToArray());
                    classSyntax = classSyntax.AddMembers(methodDeclaration);
                }
            }

            // Replace the old class in the namespace with the modified one
            namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
            var compilationUnit = CreateCompilationUnit(namespaceSyntax, graphSo.GetAllTypes());
            
            WriteFile(compilationUnit, filePath);
        }

        public static void WriteStaticMethodScript(BlueprintGraphSo graphSo, string filePath)
        {
            NodeMap.Clear();
            EntrySyntaxNode entry = null;
            foreach (var n in graphSo.DesignGraph.Current.Nodes)
            {
                var syntaxNode = BlueprintSyntaxNodeBase.ConvertToSyntaxNode(n);
                if (syntaxNode is EntrySyntaxNode esn)
                {
                    entry = esn;
                }

                NodeMap.Add(n.Guid, syntaxNode);
            }
            
            WriteGraphTemplateClass("Vapor.Blueprints.Testing", graphSo.name, out var namespaceSyntax, out var classSyntax);
            
            // Generate the method name (e.g., "CallMethodsAndReturnOutputs")
            var methodNameSyntax = SyntaxFactory.Identifier($"{graphSo.name}_Implementation");
            var parameters = CreateMethodParameters(graphSo.DesignGraph.Current.InputArguments);
            var returnType = CreateMethodReturnType(graphSo.DesignGraph.Current.OutputArguments);
            var methodDeclaration = CreateMethodDeclaration(methodNameSyntax, parameters, returnType);
            s_CurrentBody = SyntaxFactory.Block();
            s_CurrentBody = entry.AddStatementAndContinue(s_CurrentBody);
            
            // graphSo.DesignGraph.Traverse(VisitNode);
            methodDeclaration = methodDeclaration.AddBodyStatements(s_CurrentBody.Statements.ToArray());
            classSyntax = classSyntax.AddMembers(methodDeclaration);

            // Replace the old class in the namespace with the modified one
            namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
            var compilationUnit = CreateCompilationUnit(namespaceSyntax, new[] { typeof(int) });
            
            WriteFile(compilationUnit, filePath);
        }
        
        private static void VisitNode(BlueprintDesignNode node)
        {
            // Need to get all input data required for this node.
            // Need to then create the syntax for the node.
            // Need to move to the next node.
            // s_CurrentBody = AddMethodInfoToBody(mi, s_CurrentBody);
        }


        private static void WriteGraphTemplateClass(string namespaceName, string className, out NamespaceDeclarationSyntax namespaceDeclarationSyntax,
            out ClassDeclarationSyntax classDeclarationSyntax)
        {
            // Define the namespace
            namespaceDeclarationSyntax = CreateNamespace(namespaceName);

            // Define the class
            classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IBlueprintGraph")))
                .AddMembers(
                    // Implement IsEvaluating property
                    SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), "IsEvaluating")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        }))),
                    
                    // Public constructor
                    SyntaxFactory.ConstructorDeclaration(className)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithBody(SyntaxFactory.Block()),

                    // Implement Invoke method
                    SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Invoke")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("parameters"))
                                .WithType(SyntaxFactory.ParseTypeName("object[]")),
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("returnCallback"))
                                .WithType(SyntaxFactory.ParseTypeName("Action<IBlueprintGraph>"))
                        )
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ParseStatement("returnCallback?.Invoke(this);")
                        ))
                );
        }

        private static ParameterListSyntax CreateMethodParameters(List<BlueprintVariable> arguments)
        {
            // Create parameters for the input arguments
            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(arguments.Select(arg =>
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(arg.Name))
                        .WithType(SyntaxFactory.ParseTypeName(arg.Type.FullName ?? arg.Type.Name))
                ))
            );
        }

        private static TypeSyntax CreateMethodReturnType(List<BlueprintVariable> arguments)
        {
            // Generate method signature (with input arguments)
            return arguments == null || arguments.Count == 0
                ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                : SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(arguments.Select(a => a.Type).Select(t => SyntaxFactory.TupleElement(SyntaxFactory.ParseTypeName(t.FullName ?? t.Name)))));
        }

        private static MethodDeclarationSyntax CreateMethodDeclaration(SyntaxToken methodName, ParameterListSyntax parameters, TypeSyntax returnType)
        {
            return SyntaxFactory.MethodDeclaration(returnType, methodName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(parameters)
                .NormalizeWhitespace();
        }
        
        public static string GenerateMethodFromInvocation(
            string methodName,
            (string, Type)[] inputArgs, // Array of input (name, type) tuples
            Type[] outputArgs, // Array of output types
            List<MethodInfo> methodCollection) // Collection of MethodInfos to invoke
        {
            // Generate method signature (with input arguments)
            TypeSyntax returnType = (outputArgs == null || outputArgs.Length == 0)
                ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                : SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(outputArgs.Select(t => SyntaxFactory.TupleElement(SyntaxFactory.ParseTypeName(t.FullName ?? t.Name)))));
            

            // Generate the method name (e.g., "CallMethodsAndReturnOutputs")
            var methodNameSyntax = SyntaxFactory.Identifier(methodName);

            // Create parameters for the input arguments
            var parameters = SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(inputArgs.Select(arg =>
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(arg.Item1))
                        .WithType(SyntaxFactory.ParseTypeName(arg.Item2.FullName ?? arg.Item2.Name))
                ))
            );

            // Create method declaration
            var methodDecl = SyntaxFactory.MethodDeclaration(returnType, methodNameSyntax)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(parameters)
                .NormalizeWhitespace();

            // Generate the body of the method (input initialization, method invocations, return)
            var methodBody = SyntaxFactory.Block();

            // // Add local variable declarations for inputs
            // foreach (var (argName, argType) in inputArgs)
            // {
            //     var localDecl = SyntaxFactory.LocalDeclarationStatement(
            //         SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(argType.FullName ?? argType.Name))
            //             .WithVariables(SyntaxFactory.SingletonSeparatedList(
            //                 SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(argName))
            //             ))
            //     );
            //     methodBody = methodBody.AddStatements(localDecl);
            // }

            // Add method invocations
            int idx = 0;
            foreach (var method in methodCollection)
            {
                // Create the arguments (method parameters)
                var arguments = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(method.GetParameters().Select(p =>
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{idx}_" + p.Name))
                    ))
                );

                // If the method is static, include the declaring type in the invocation
                if (method.IsStatic)
                {
                    // Get the fully qualified name (namespace + class name)
                    string fullMethodName = method.DeclaringType.Namespace + "." + method.DeclaringType.Name;
                    var declaringType = SyntaxFactory.IdentifierName(fullMethodName);
                    var invocationExpr = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            declaringType,
                            SyntaxFactory.IdentifierName(method.Name)
                        ),
                        arguments
                    );
                    
                    var invocationStatement = SyntaxFactory.ExpressionStatement(invocationExpr);
                    methodBody = methodBody.AddStatements(invocationStatement);
                }
                else
                {
                    // If the method is not static, just use the method name (it can be an instance method)
                    var instanceExpression = SyntaxFactory.IdentifierName($"fieldName_{idx}");
                    var invocationExpr = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instanceExpression,
                            SyntaxFactory.IdentifierName(method.Name)
                        ),
                        arguments
                    );
                    
                    var invocationStatement = SyntaxFactory.ExpressionStatement(invocationExpr);
                    methodBody = methodBody.AddStatements(invocationStatement);
                }

                idx++;
            }

            // Add return statement (if outputArgs is not null or empty)
            if (outputArgs != null && outputArgs.Length > 0)
            {
                var returnTupleElements = outputArgs.Select((type, index) =>
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"output{index}"))
                ).ToArray();

                var returnTuple = SyntaxFactory.TupleExpression(
                    SyntaxFactory.SeparatedList(returnTupleElements)
                );
        
                var returnStatement = SyntaxFactory.ReturnStatement(returnTuple);
                methodBody = methodBody.AddStatements(returnStatement);
            }
            else
            {
                // // If no output arguments, return void
                // var returnStatement = SyntaxFactory.ReturnStatement();
                // methodBody = methodBody.AddStatements(returnStatement);
            }

            // Combine everything into the final method
            var fullMethod = methodDecl.AddBodyStatements(methodBody.Statements.ToArray());
    
            // Return the method code as a string
            return fullMethod.NormalizeWhitespace().ToFullString();
        }

        #region - Compilation Helpers -

        private static CompilationUnitSyntax CreateCompilationUnit(NamespaceDeclarationSyntax namespaceDeclaration, IEnumerable<Type> allTypes)
        {
            // Create the compilation unit (file)
            // var usingSyntaxes = new UsingDirectiveSyntax[usings.Length];
            // for (var i = 0; i < usings.Length; i++)
            // {
            //     usingSyntaxes[i] = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usings[i]));
            // }
            var usingSyntaxes = AddUsingsForTypes(allTypes);

            return SyntaxFactory.CompilationUnit()
                .AddUsings(usingSyntaxes)
                .AddMembers(namespaceDeclaration)
                .NormalizeWhitespace();
        }
        
        public static UsingDirectiveSyntax[] AddUsingsForTypes(IEnumerable<Type> types)
        {
            // Extract distinct namespaces from the types
            var namespaces = types
                .Select(t => t.Namespace) // Get namespaces
                .Where(ns => !string.IsNullOrEmpty(ns)) // Remove null/empty ones
                .Distinct(); // Remove duplicates

            // Create UsingDirectiveSyntax for each namespace
            var usingDirectives = namespaces.Select(ns =>
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
            );

            // Add using statements to the compilation unit
            return usingDirectives.ToArray();
        }

        private static void WriteFile(CompilationUnitSyntax compilationUnit, string filePath)
        {
            // Convert to string
            var code = compilationUnit.NormalizeWhitespace().ToFullString();

            // Write to file
            Debug.Log($"Writing File To: {filePath}");
            Debug.Log(code);
            File.WriteAllText(filePath, code);
        }
        
        public static string GetFullCSharpPath(ScriptableObject scriptableObject)
        {
            // Get the asset path (relative to "Assets/")
            string assetPath = AssetDatabase.GetAssetPath(scriptableObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("The ScriptableObject is not an asset file. Save it to the project first.");
                return string.Empty;
            }

            // Convert to absolute system path
            string fullFolderPath = Path.Combine(Application.dataPath, assetPath[7..]); // Remove "Assets/" prefix

            // Ensure the directory exists
            string folderPath = Path.GetDirectoryName(fullFolderPath);
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("Folder path does not exist: " + folderPath);
                return string.Empty;
            }

            // Construct the full file path
            return Path.Combine(folderPath, $"{scriptableObject.name}.cs");
        }
        #endregion
        
        #region - Class Generation Helpers -

        private static void WriteEmptyClass(string namespaceName, string typeName, string baseTypeName, out NamespaceDeclarationSyntax namespaceDeclarationSyntax, out ClassDeclarationSyntax classDeclarationSyntax)
        {
            // Define the namespace
            namespaceDeclarationSyntax = CreateNamespace(namespaceName);

            // Define the class
            classDeclarationSyntax = SyntaxFactory.ClassDeclaration(typeName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseTypeName)))
                .AddMembers(
                    // Public constructor
                    SyntaxFactory.ConstructorDeclaration(typeName)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithBody(SyntaxFactory.Block())
                );
        }

        private static NamespaceDeclarationSyntax CreateNamespace(string @namespace)
        {
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
                .NormalizeWhitespace();
        }

        #endregion

        #region - Method Generation Helpers -
        internal static TypeSyntax GetTypeSyntax(Type type)
        {
            if (type.IsGenericType)
            {
                // Extract the base generic type name (e.g., "List" from "List<int>")
                string baseTypeName = type.Name[..type.Name.IndexOf('`')]; // Remove generic arity (`1, `2, etc.)

                // Recursively get type syntax for each generic argument
                var genericArguments = type.GetGenericArguments()
                    .Select(GetTypeSyntax) // Recursively resolve nested generics
                    .ToArray();

                return SyntaxFactory.GenericName(baseTypeName)
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(genericArguments)));
            }

            // Handle arrays
            if (type.IsArray)
            {
                return SyntaxFactory.ArrayType(GetTypeSyntax(type.GetElementType()))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()
                            )
                        )
                    ));
            }

            // Convert known types to short names (e.g., "int" instead of "System.Int32")
            return GetFriendlyTypeName(type);
        }
        
        internal static TypeSyntax GetFriendlyTypeName(Type type)
        {
            // Check for void
            if (type == typeof(void))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }

            // Handle primitive and common types
            string typeName = type.FullName switch
            {
                "System.Int32" => "int",
                "System.Boolean" => "bool",
                "System.Single" => "float",
                "System.Double" => "double",
                "System.Int16" => "short",
                "System.Int64" => "long",
                "System.Char" => "char",
                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.UInt16" => "ushort",
                "System.UInt32" => "uint",
                "System.UInt64" => "ulong",
                "System.String" => "string",
                "System.Object" => "object",
                _ => type.Name // Fallback for custom types
            };

            return SyntaxFactory.ParseTypeName(typeName);
        }

        internal static ExpressionSyntax GetDefaultValueSyntax(Type type)
        {
            if (type.IsValueType)
            {
                if (type == typeof(bool))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression); // false
                if (type == typeof(char))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal('\0')); // '\0'
                if (type == typeof(float))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0f)); // 0.0f
                if (type == typeof(double))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0)); // 0.0
                if (type == typeof(int))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)); // 0
                if (type == typeof(long))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0L)); // 0L
                if (type == typeof(short))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((short)0)); // (short)0
                if (type == typeof(byte))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((byte)0)); // (byte)0
                if (type == typeof(sbyte))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((sbyte)0)); // (sbyte)0
                if (type == typeof(uint))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0U)); // 0U
                if (type == typeof(ulong))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0UL)); // 0UL
                if (type == typeof(ushort))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ushort)0)); // (ushort)0
                if (type == typeof(decimal))
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0m)); // 0m

                // Default struct initializer (e.g., new MyStruct())
                return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(type.Name));
            }

            // For reference types (classes, interfaces, delegates, object, etc.), return null
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        internal static ExpressionSyntax GetExpressionForObjectInitializer(object value)
        {
            if (value == null)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression); // null

            Type type = value.GetType();

            // Handle primitive types with literal expressions
            if (value is int i) return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i));
            if (value is float f) return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(f));
            if (value is double d) return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(d));
            if (value is bool b) return SyntaxFactory.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            if (value is string s) return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s));

            // Handle structs and classes with object initializer syntax
            var propertyAssignments = type.GetProperties()
                .Where(p => p.CanRead && p.CanWrite) // Only writable properties
                .Select(p =>
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(p.Name), // Property name
                        GetExpressionForObjectInitializer(p.GetValue(value)) // Recursively assign property values
                    )
                )
                .ToArray();

            if (propertyAssignments.Length > 0)
            {
                return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type.Name))
                    .WithInitializer(
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ObjectInitializerExpression,
                            SyntaxFactory.SeparatedList<ExpressionSyntax>(propertyAssignments)
                        )
                    );
            }

            // If no writable properties, return default struct initialization
            return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(type.Name));
        }

        internal static BlockSyntax AddMethodInfoToBody(string variableName, string[] argumentNames, string invokingVariableName, MethodInfo method, BlockSyntax methodBody)
        {
            // Create the arguments (method parameters)
            var arguments = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(argumentNames.Select(arg =>
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(arg))
                ))
            );

            // Determine the invocation expression
            ExpressionSyntax invocationExpr;

            if (method.IsStatic)
            {
                // Fully qualified name (Namespace + Class)
                string fullMethodName = $"{method.DeclaringType.Namespace}.{method.DeclaringType.Name}";
                var declaringType = SyntaxFactory.IdentifierName(fullMethodName);

                invocationExpr = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        declaringType,
                        SyntaxFactory.IdentifierName(method.Name)
                    ),
                    arguments
                );
            }
            else
            {
                // Instance method invocation
                if (invokingVariableName.EmptyOrNull())
                {
                    invocationExpr = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(method.Name),
                        arguments
                    );
                }
                else
                {
                    // Instance method invocation
                    invocationExpr = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(invokingVariableName), // Assuming 'instance' is available
                            SyntaxFactory.IdentifierName(method.Name)
                        ),
                        arguments
                    );
                }
            }

            // If method returns void, just execute it
            if (method.ReturnType == typeof(void))
            {
                methodBody = methodBody.AddStatements(SyntaxFactory.ExpressionStatement(invocationExpr));
            }
            else
            {
                // Choose whether to use `var` or an explicit type
                TypeSyntax returnTypeSyntax = GetTypeSyntax(method.ReturnType);
                bool useVar = method.ReturnType.IsGenericType || method.ReturnType == typeof(object);

                // Variable declaration using either `var` or explicit type
                VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(
                    useVar ? SyntaxFactory.IdentifierName("var") : returnTypeSyntax
                ).AddVariables(
                    SyntaxFactory.VariableDeclarator(variableName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(invocationExpr)) // Assign invocation result
                );

                // Add the variable declaration to the method body
                methodBody = methodBody.AddStatements(SyntaxFactory.LocalDeclarationStatement(variableDeclaration));
            }

            return methodBody;
        }

        #endregion
    }
}
