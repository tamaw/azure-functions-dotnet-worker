﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixForRegistrationInASPNetCoreIntegration)), Shared]
    public sealed class CodeFixForRegistrationInASPNetCoreIntegration : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(new ChangeConfigurationForASPNetIntegration(context.Document, diagnostic), diagnostic);

            return Task.CompletedTask;
        }

        /// <summary>
        /// CodeAction implementation which fixes method configuration for ASP.NET Core Integration.
        /// </summary>
        private sealed class ChangeConfigurationForASPNetIntegration : CodeAction
        {
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            internal ChangeConfigurationForASPNetIntegration(Document document, Diagnostic diagnostic)
            {
                this._document = document;
                this._diagnostic = diagnostic;
            }

            public override string Title => "Change configuration for ASP.Net Core Integration";

            /// <summary>
            /// Returns an updated Document where the async method return type is changed from void to Task.
            /// </summary>
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxTree = await _document.GetSyntaxTreeAsync();

                var currentNode = syntaxTree.GetRoot().FindNode(this._diagnostic.Location.SourceSpan)
                                                                .FirstAncestorOrSelf<IdentifierNameSyntax>();

                var newNode = currentNode.ReplaceNode(currentNode, SyntaxFactory.IdentifierName(ExpectedRegistrationMethod));

                var newSyntaxRoot = syntaxTree.GetRoot().ReplaceNode(currentNode, newNode);
                return _document.WithSyntaxRoot(newSyntaxRoot);
            }
        }
    }
}
