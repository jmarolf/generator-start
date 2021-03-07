using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Analyzer;
using System.Diagnostics;

namespace CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypesShouldBeUpperCaseFixer)), Shared]
    public class TypesShouldBeUpperCaseFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(TypesShouldBeUpperCaseAnalyzer.DiagnosticId);

        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md
        // for more information on Fix All Providers
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            if (declaration is null)
            {
                // We cannot find the type declaration node so do not offer a fix.
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make uppercase",
                    createChangedSolution: c => MakeUppercaseAsync(context.Document, declaration, c),
                    equivalenceKey: "Make uppercase"),
                diagnostic);
        }

        private static async Task<Solution> MakeUppercaseAsync(Document document,
                                                               TypeDeclarationSyntax typeDeclaration,
                                                               CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDeclaration.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            
            // We don't expect this to ever be null since our analyzer gave us this location
            Debug.Assert(typeSymbol is not null);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(originalSolution,
                                                              typeSymbol,
                                                              newName,
                                                              optionSet,
                                                              cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}
