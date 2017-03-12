using System;
using System.IO;
using PT.PM;
using PT.PM.Common;
using PT.PM.Common.CodeRepository;
using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Expressions;
using PT.PM.Dsl;
using PT.PM.Matching;
using PT.PM.ParseTreeUst;
using PT.PM.Patterns;
using PT.PM.UstParsing;
using PT.PM.Patterns.Nodes;
using System.Linq;
using System.Collections.Generic;

namespace basicPM
{
	class MainClass
	{
		static string sourcesFolder = "";

		static string filePath = "code.php";

		public static void Main(string[] args)
		{
			Console.WriteLine("Что же у нас в " + filePath);
			//Создаем файловый репозиторий и читаем содержимое файла
			var filecoderepository = new FileCodeRepository(sourcesFolder + filePath);
			var file = filecoderepository.ReadFile();

			//Выбираем php парсер и парсим им код
			var parser = new PhpAntlrParser();
			var parsetree = parser.Parse(file);

			//Создаем конвертер и преобразуем распаршенные ANTLR токены в universal AST
			var converter = new PhpAntlrParseTreeConverter();
			var uast = converter.Convert(parsetree);

			//Получаем паттерны, по которым будем искать узлы в дереве
			var patternsData = File.ReadAllText("patterns.json");
			var patternsRepository = new StringPatternsRepository(patternsData);
			var patternDtos = patternsRepository.GetAll();

			//Конвертируем паттерны
			IUstNodeSerializer jsonNodeSerializer = new JsonUstNodeSerializer(typeof(UstNode), typeof(PatternVarDef));
			IUstNodeSerializer dslNodeSerializer = new DslProcessor();
			var patternConverter = new CommonPatternConverter(new IUstNodeSerializer[] { jsonNodeSerializer, dslNodeSerializer });

			//Получаем найденные узлы
			var matcher = new BruteForcePatternMatcher(patternConverter.Convert(patternDtos));
            var matches = matcher.Match(uast).ToArray();

			//Словарь ключ: номер строки, значение: что встретилось на ней (функция/массив/приведение к типу)
			var lineTokenDict = new Dictionary<string, string>();

			for (int i = 0; i < matches.Length; i++)
			{
				var matchedNode = matches[i].Nodes.Last();
				var token = "";
				//Получаем всех потомков первого уровня
				matchedNode.GetAllDescendants(child =>
					{
						//Если тип ноды функция
						if (child.NodeType == NodeType.InvocationExpression)
						{
							token = token + "Функция: " + ((InvocationExpression)child).Target;
						}
						
						//Если тип ноды массив
						if (child.NodeType == NodeType.IndexerExpression)
						{
							token = token + "Массивы с ПВ: " + ((IndexerExpression)child).Target;
						}
						
						//Если тип ноды приведение к типу
						if (child.NodeType == NodeType.CastExpression)
						{
							token = token + "Приведение к типу: " + ((CastExpression)child).Type;
						}

						//Если после условий токен не пустой, то получаем номер строки из линейного значения
						//и чистим строку с токеном после занесения в словарь
						if (!token.Equals(""))
						{
							int line, column;
							TextHelper.LinearToLineColumn(child.TextSpan.Start, file.Code, out line, out column);
							lineTokenDict[line.ToString()] = lineTokenDict.ContainsKey(line.ToString()) ? lineTokenDict[line.ToString()] + ", " + token : token;
							token = "";
						}
					  	return true;
					});

			}

			//Выводим в консоль сформированный словарь
			foreach (var line in lineTokenDict.Keys)
			{
				Console.WriteLine(String.Format("{0}: {1}",line,lineTokenDict[line]));
			}
			Console.ReadLine();
		}
	}
}
