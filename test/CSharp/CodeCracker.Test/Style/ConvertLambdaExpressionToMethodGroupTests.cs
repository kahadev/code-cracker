﻿using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ConvertLambdaExpressionToMethodGroupTests
        : CodeFixTest<ConvertLambdaExpressionToMethodGroupAnalyzer, ConvertLambdaExpressionToMethodGroupFixProvider>
    {
        [Fact]
        public async Task CreateDiagnosticForSimpleLambdaExpression()
        {
            var test = @"var f = a.Where(item => filter(item));";
            var expected = new DiagnosticResult
            {
                Id = ConvertLambdaExpressionToMethodGroupAnalyzer.DiagnosticId,
                Message = "You should remove the lambda expression and pass just 'filter' instead.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticForSimpleLambdaExpressionWithBlockInBody()
        {
            var test = @"var f = a.Where(item => { return filter(item); });";
            var expected = new DiagnosticResult
            {
                Id = ConvertLambdaExpressionToMethodGroupAnalyzer.DiagnosticId,
                Message = "You should remove the lambda expression and pass just 'filter' instead.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticForParenthesizedLambdaExpressionWithBlockInBody()
        {
            var test = @"var f = a.Foo((param1, param2) => { return filter(param1, param2); });";
            var expected = new DiagnosticResult
            {
                Id = ConvertLambdaExpressionToMethodGroupAnalyzer.DiagnosticId,
                Message = "You should remove the lambda expression and pass just 'filter' instead.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 15) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task DoNotCreateDiagnosticForMethodGoupd()
        {
            var test = @"var f = a.Where(filter);";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task DoNotCreateDiagnosticWhenParametersDoNotMatch()
        {
            var test = @"var f = a.Foo((param1, param2) => { return filter(param2, param1); });";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task DoNotCreateDiagnosticWhenIncompleteLambdaExpression()
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, bool> a = x => filter();
        }
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task SimpleLambdaExpressionIsReplaceByMethodInDeclarationStatement()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, bool> a = x => filter(x);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, bool> a = filter;
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task ParenthesizedLambdaExpressionIsReplaceByMethodInDeclarationStatement()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int param1, int param2)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, int, bool> a = (x, y) => filter(x, y);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int param1, int param2)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            Func<int, int, bool> a = filter;
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task SimpleLambdaExpressionIsReplaceByMethodInArgumentList()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(item => filter(item));
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task ParenthesizedLambdaExpressionIsReplaceByMethodInArgumentList()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value, int index)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where((item, index) => filter(item, index));
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value, int index)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task SimpleLambdaExpressionWithBlockInBodyIsReplaceByMethodInArgumentList()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(item => { return filter(item); });
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task ParenthesizedLambdaExpressionWihtBlockInBodyIsReplaceByMethodInArgumentList()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value, int index)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where((item, index) => { return filter(item, index); });
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value, int index)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task FixEndOfPipelineLambdaExpressionAndReplaceByMethod()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => filter(item));
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task FixMiddleOfPipelineLambdaExpressionAndReplaceByMethod()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => filter(item)).Select(item => item * 2);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(filter).Select(item => item * 2);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }


        [Fact]
        public async Task FixMiddleOfPipelineLambdaExpressionAndReplaceByMethodMultipleMatches()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        private int orderAccessor(int value)
        {
            return item;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => orderAccessor(item)).Where(item => filter(item)).Select(item => item * 2);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private bool filter(int value)
        {
            return true;
        }

        private int orderAccessor(int value)
        {
            return item;
        }

        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(orderAccessor).Where(filter).Select(item => item * 2);
        }
    }
}";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task FixMiddleOfPipelineLambdaExpressionAndReplaceByMethodMultipleMatchesWithThis()
        {
            var oldCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private int orderAccessor(int value)
        {
            return item;
        }
        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(item => this.orderAccessor(item)).Select(item => item * 2);
        }
    }
}";

            var newCode = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        private int orderAccessor(int value)
        {
            return item;
        }
        public void bar()
        {
            var a = new int[10];
            var f = a.OrderBy(this.orderAccessor).Select(item => item * 2);
        }
    }
}";
            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task DoNotCreateDiagnosticWhenSubstitutionMayBreakInvocationResolution()
        {
            var oldCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication4
{
    class Test
    {
        protected virtual Task<HttpResponseMessage> SendImplAsyncWorks(CancellationToken cancellationToken)
        {
            return HttpClient.SendAsync().Finally(task => OnAfterAsyncWebResponse(task));
        }
        protected virtual Task<HttpResponseMessage> SendImplAsyncBreaks(CancellationToken cancellationToken)
        {
            return HttpClient.SendAsync().Finally(OnAfterAsyncWebResponse);
        }
        protected virtual void OnAfterAsyncWebResponse(Task<HttpResponseMessage> response) { }
    }

    public class HttpClient
    {
        public static Task<HttpResponseMessage> SendAsync()
        {
            throw new NotImplementedException();
        }
    }
    public class HttpResponseMessage { }

    public static class Extension
    {
        public static Task<TResult> Finally<TResult>(this Task<TResult> task, Action<Task<TResult>> cleanupAction)
        {
            throw new NotImplementedException();
        }
        public static Task<TResult> Finally<TResult>(this Task<TResult> task, Func<Task<TResult>, Task> cleanupAction)
        {
            throw new NotImplementedException();
        }
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(oldCode);
        }
    }
}