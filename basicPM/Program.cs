using System;
using System.Linq;
using PT.PM;
using PT.PM.Common;
using PT.PM.Common.CodeRepository;
using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Expressions;
using PT.PM.Patterns.PatternsRepository;
using System.Collections.Generic;

namespace basicPM
{
    class MainClass
    {
        static void Main(string[] args)
        {
            var fileRepository = new FileCodeRepository("code.php");
            var patternsRepostiory = new FilePatternsRepository("patterns.json");

            var workflow = new Workflow(fileRepository, patternsRepostiory);
            workflow.IsIncludeIntermediateResult = true;

            WorkflowResult result = workflow.Process();

            var lineTokenDict = new Dictionary<int, List<string>>();
            foreach (var matchingResult in result.MatchingResults)
            {
                var matchedNode = matchingResult.Nodes.Last();
                UstNode[] descendants = matchedNode.GetAllDescendants();

                foreach (UstNode descendant in descendants)
                {
                    string text = null;
                    if (descendant.NodeType == NodeType.InvocationExpression)
                        text = "Method invocation: " + ((InvocationExpression)descendant).Target;
                    else if (descendant.NodeType == NodeType.IndexerDeclaration)
                        text = "Arrays with user input: " + ((IndexerExpression)descendant).Target;
                    else if (descendant.NodeType == NodeType.CastExpression)
                        text = "Cast to type: " + ((CastExpression)descendant).Type;

                    if (!string.IsNullOrEmpty(text))
                    {
                        SourceCodeFile sourceCodeFile = result.SourceCodeFiles.Single(file => file.FullPath == descendant.FileNode.FileName.Text);
                        LineColumnTextSpan lineColumnTextSpan = sourceCodeFile.GetLineColumnTextSpan(descendant.TextSpan);
                        List<string> strs;
                        if (!lineTokenDict.TryGetValue(lineColumnTextSpan.BeginLine, out strs))
                        {
                            strs = new List<string>();
                            lineTokenDict[lineColumnTextSpan.BeginLine] = strs;
                        }
                        strs.Add(text);
                    }
                }
            }
            
            foreach (var item in lineTokenDict)
            {
                Console.WriteLine(String.Format("{0}: {1}", item.Key, string.Join(", ", item.Value)));
            }

            Console.ReadLine();
        }
    }
}