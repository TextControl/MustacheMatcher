 	namespace TXTextControl.DocumentServer.Fields
	{
		using System.Collections.Generic;
		using System.Text.RegularExpressions;
		using TXTextControl;
	
		public class TagInfo
		{
			public int StartIndex { get; set; }
			public int EndIndex { get; set; }
		}
	
		public class FieldInfo : TagInfo
		{
			public string FieldName { get; set; }
		}
	
		public class BlockInfo : TagInfo
		{
			public string BlockName { get; set; }
		}
	
		public class MustacheMatcher
		{
			public static void Convert(ServerTextControl textControl)
			{
				var text = textControl.Text.Replace("\r\n", "\n");
	
				// Process merge fields
				foreach (var field in FindMergeFields(text))
				{
					ReplaceWithMergeField(textControl, field);
				}
	
				// Process merge blocks
				foreach (var block in FindMergeBlocks(text))
				{
					AddSubTextPart(textControl, block);
				}
	
				// Process special elements
				RemoveSpecialElements(textControl, text);
			}
	
			private static void ReplaceWithMergeField(ServerTextControl textControl, FieldInfo field)
			{
				textControl.Select(field.StartIndex, field.EndIndex - field.StartIndex);
				var selectionText = textControl.Selection.Text;
	
				var mergeField = new MergeField
				{
					Name = field.FieldName,
					Text = selectionText,
					ApplicationField =
					{
						DoubledInputPosition = true,
						HighlightMode = HighlightMode.Activated
					}
				};
	
				textControl.Selection.Text = string.Empty;
				textControl.ApplicationFields.Add(mergeField.ApplicationField);
			}
	
			private static void AddSubTextPart(ServerTextControl textControl, BlockInfo block)
			{
				var subTextPart = new SubTextPart("txmb_" + block.BlockName, 1, block.StartIndex, block.EndIndex - block.StartIndex);
				textControl.SubTextParts.Add(subTextPart);
			}
	
			private static void RemoveSpecialElements(ServerTextControl textControl, string text)
			{
				var matchElements = FindSpecialElements(text);
	
				int indexOffset = 0;
	
				foreach (var tag in matchElements)
				{
					textControl.Select(tag.StartIndex - indexOffset, tag.EndIndex - tag.StartIndex);
					indexOffset += textControl.Selection.Length;
					textControl.Selection.Text = string.Empty;
				}
			}
	
			private static List<FieldInfo> FindMergeFields(string input)
			{
				const string pattern = @"\{\{(?!#|/)(.*?)\}\}";
				var matches = new List<FieldInfo>();
	
				foreach (Match match in Regex.Matches(input, pattern))
				{
					matches.Add(new FieldInfo
					{
						StartIndex = match.Index,
						EndIndex = match.Index + match.Length,
						FieldName = Regex.Replace(match.Groups[1].Value, @"\s+", "")
					});
				}
	
				return matches;
			}
	
			private static List<TagInfo> FindSpecialElements(string input)
			{
				const string pattern = @"\{\{(#|\/)(.*?)\}\}";
				var matches = new List<TagInfo>();
	
				foreach (Match match in Regex.Matches(input, pattern))
				{
					matches.Add(new TagInfo
					{
						StartIndex = match.Index,
						EndIndex = match.Index + match.Length
					});
				}
	
				return matches;
			}
	
			private static List<BlockInfo> FindMergeBlocks(string input)
			{
				const string pattern = @"\{\{(#foreach\s+\w+|\s*\/foreach\s+\w+)\}\}";
				var matches = new List<BlockInfo>();
				var stack = new Stack<Match>();
	
				foreach (Match match in Regex.Matches(input, pattern))
				{
					if (match.Value.StartsWith("{{#foreach"))
					{
						stack.Push(match);
					}
					else if (match.Value.StartsWith("{{/foreach") && stack.Count > 0)
					{
						var startMatch = stack.Pop();
						var startVar = ExtractVariableName(startMatch.Value);
						var endVar = ExtractVariableName(match.Value);
	
						if (startVar == endVar)
						{
							matches.Add(new BlockInfo
							{
								StartIndex = startMatch.Index + 1,
								EndIndex = match.Index + 1 + match.Length,
								BlockName = startVar
							});
						}
					}
				}
	
				matches.Sort((x, y) => x.StartIndex.CompareTo(y.StartIndex));
	
				return matches;
			}
	
			private static string ExtractVariableName(string tag)
			{
				var startIndex = tag.StartsWith("{{#foreach") ? 10 : 11;
				var length = tag.Length - startIndex - 2;
				return tag.Substring(startIndex, length).Trim();
			}
		}
	}
