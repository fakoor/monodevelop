﻿//
// ParameterUtil.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class ParameterIndexResult
	{
		public readonly static ParameterIndexResult Invalid = new ParameterIndexResult (null, -1);
		public readonly static ParameterIndexResult First   = new ParameterIndexResult (null, 0);
		
		public readonly string[] UsedNamespaceParameters;
		public readonly int      ParameterIndex;
		
		
		internal ParameterIndexResult(string[] usedNamespaceParameters, int parameterIndex)
		{
			UsedNamespaceParameters = usedNamespaceParameters;
			ParameterIndex = parameterIndex;
		}
	}
		
	public static class ParameterUtil
	{
		public static async Task<ParameterIndexResult> GetCurrentParameterIndex (Document document, int startOffset, int caretOffset, CancellationToken cancellationToken = default(CancellationToken))
		{
			List<string> usedNamedParameters = null;
			var tree = await document.GetSyntaxTreeAsync (cancellationToken).ConfigureAwait (false);
			var root = await tree.GetRootAsync (cancellationToken).ConfigureAwait (false);

			var token = root.FindToken (startOffset);
			if (token.Parent == null)
				return ParameterIndexResult.Invalid;

			var invocation = token.Parent.AncestorsAndSelf ().OfType<InvocationExpressionSyntax> ().FirstOrDefault ();

			if (invocation == null || invocation.ArgumentList == null)
				return ParameterIndexResult.Invalid;

			int i = 0;
			int j = 0;
			foreach (var child in invocation.ArgumentList.ChildNodesAndTokens ()) {
				if (child.Span.End > caretOffset) {
					if (i == 0 && j <= 1)
						return ParameterIndexResult.First;
					return new ParameterIndexResult (usedNamedParameters != null ? usedNamedParameters.ToArray () : null, i + 1);
				}

				if (child.IsToken) {
					if (child.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.CommaToken))
						i++;
				} else {
					var node = child.AsNode () as ArgumentSyntax;
					if (node != null && node.NameColon != null) {
						if (usedNamedParameters == null)
							usedNamedParameters = new List<string> ();
						usedNamedParameters.Add (node.NameColon.Name.Identifier.Text);
					}
				}
				j++;
			}
			if (j > 0) {
				return new ParameterIndexResult (usedNamedParameters != null ? usedNamedParameters.ToArray () : null, i + 1);
			}
			return ParameterIndexResult.Invalid;
		}
	}
}
