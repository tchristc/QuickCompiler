using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace QuickCompiler
{
    public interface ISyntaxTreeProvider : IProvider<SyntaxTree>
    {
        ISyntaxTreeProvider WithCode(string code);
    }

    public class SyntaxTreeProvider : ISyntaxTreeProvider
    {
        private string _code;

        public ISyntaxTreeProvider WithCode(string code)
        {
            _code = code;
            return this;
        }


        public SyntaxTree Provide()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(_code);
            return syntaxTree;
        }
    }

    public interface INamespaceProvider : IProvider<IEnumerable<string>>
    {
        
    }

    public class DefaultNamespaceProvider : INamespaceProvider
    {
        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
                "System",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Collections.Generic"
            };

        public IEnumerable<string> Provide()
        {
            return DefaultNamespaces;
        }
    }

    public class DefaultWithAdditionalNamespaceProvider : INamespaceProvider
    {
        private readonly List<string> _additionalNamespaces;
        private readonly INamespaceProvider _defaultNamespaceProvider;

        public DefaultWithAdditionalNamespaceProvider(IEnumerable<string> additionalNamespaces)
        {
            _additionalNamespaces = additionalNamespaces.ToList();
            _defaultNamespaceProvider = new DefaultNamespaceProvider();
        }

        public IEnumerable<string> Provide()
        {
            _additionalNamespaces.AddRange(_defaultNamespaceProvider.Provide());
            return _additionalNamespaces;
        }
    }

    public interface ICodeProvider : IProvider<string>
    {

    }

    public class CodeProvider : ICodeProvider
    {
        private readonly string _code;

        public CodeProvider(string code)
        {
            _code = code;
        }

        public string Provide()
        {
            return _code;
        }
    }

    public interface IFileNameProvider: IProvider<string>
    {

    }

    public class RandomFileNameProvider : IFileNameProvider
    {
        private string _filename;

        public string Provide()
        {
            _filename = Path.GetRandomFileName();
            return _filename;
        }
    }

    public interface IReferenceProvider : IProvider<IEnumerable<MetadataReference>>
    {

    }

    public class DefaultReferenceProvider : IReferenceProvider
    {
        public IEnumerable<MetadataReference> Provide()
        {
            return new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };
        }
    }

    public class DefaultWithAdditionalReferenceProvider : IReferenceProvider
    {
        private readonly DefaultReferenceProvider _defaultReferenceProvider = new DefaultReferenceProvider();
        private readonly List<MetadataReference> _additionalMetadataReferences;

        public DefaultWithAdditionalReferenceProvider(List<MetadataReference> additionalMetadataReferences)
        {
            _additionalMetadataReferences = additionalMetadataReferences;
        }


        public IEnumerable<MetadataReference> Provide()
        {
            _additionalMetadataReferences.AddRange(_defaultReferenceProvider.Provide());
            return _additionalMetadataReferences;
        }
    }

    public interface ICompilationOptionProvider : IProvider<CSharpCompilationOptions>
    {
        
    }

    public class DefaultCompilationOptionProvider : ICompilationOptionProvider
    {
        private readonly INamespaceProvider _namespaceProvider;

        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release);

        public DefaultCompilationOptionProvider(INamespaceProvider namespaceProvider)
        {
            _namespaceProvider = namespaceProvider;
           
        }

        public CSharpCompilationOptions Provide()
        {
            var compilationOptions = DefaultCompilationOptions.WithUsings(_namespaceProvider.Provide());
            return compilationOptions;
        }
    }

    public interface ICompilationProvider : IProvider<Compilation>
    {
        
    }

    public class CompilationProvider : ICompilationProvider
    {
        public Compilation Provide()
        {
            throw new NotImplementedException();
        }
    }

    public class CsharpCompilationProvider : ICompilationProvider
    {
        private readonly ISyntaxTreeProvider _syntaxTreeProvider;
        private readonly ICodeProvider _codeProvider;
        private readonly IFileNameProvider _fileNameProvider;
        private readonly IReferenceProvider _referenceProvider;
        private readonly ICompilationOptionProvider _compilationOptionProvider;

        public CsharpCompilationProvider(ISyntaxTreeProvider syntaxTreeProvider,
            ICodeProvider codeProvider,
            IFileNameProvider fileNameProvider,
            IReferenceProvider referenceProvider,
            ICompilationOptionProvider compilationOptionProvider)
        {
            _syntaxTreeProvider = syntaxTreeProvider;
            _codeProvider = codeProvider;
            _fileNameProvider = fileNameProvider;
            _referenceProvider = referenceProvider;
            _compilationOptionProvider = compilationOptionProvider;
        }

        public Compilation Provide()
        {
            var code = _codeProvider
                .Provide();

            var syntaxTree = _syntaxTreeProvider
                .WithCode(code)
                .Provide();

            var assemblyName = _fileNameProvider.Provide();
            var references = _referenceProvider.Provide();
            var compilationOptions = _compilationOptionProvider.Provide();

            var compilation = CSharpCompilation.Create(
                    assemblyName,
                    new[] { syntaxTree },
                    references,
                    compilationOptions);

            return compilation;
        }
    }

    public interface ICompiler
    {
        Assembly Compile();
    }

    public class MemoryCompiler
    {
        private readonly ICompilationProvider _compilationProvider;

        public MemoryCompiler(ICompilationProvider compilationProvider)
        {
            _compilationProvider = compilationProvider;
        }

        public Assembly Compile()
        {
            var compilation = _compilationProvider.Provide();

            using (var ms = new MemoryStream())
            {

                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    return assembly;
                }
            }
            return null;
        }
    }

    public class DefaultCSharpMemoryCompiler : ICompiler
    {
        private readonly MemoryCompiler _compiler;

        public DefaultCSharpMemoryCompiler(string code)
        {
            _compiler = new MemoryCompiler(
                new CsharpCompilationProvider(
                    new SyntaxTreeProvider(),
                    new CodeProvider(code),
                    new RandomFileNameProvider(),
                    new DefaultReferenceProvider(),
                    new DefaultCompilationOptionProvider(
                        new DefaultNamespaceProvider())));
        }

        public Assembly Compile()
        {
            return _compiler.Compile();
        }
    }
}
