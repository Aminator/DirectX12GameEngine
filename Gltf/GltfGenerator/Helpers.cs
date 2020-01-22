using System.CodeDom;
using System.Text;

namespace GltfGenerator
{
    public static class Helpers
    {
        public static string GetFieldName(string name)
        {
            return "_" + char.ToLower(name[0]) + name.Substring(1);
        }

        public static string ToPascalCase(string name)
        {
            if (name.Contains(" ") || name.Contains("_") || name == name.ToUpper())
            {
                var words = name.ToLower().Split(' ', '_');

                StringBuilder sb = new StringBuilder();

                foreach (string word in words)
                {
                    sb.Append(char.ToUpper(word[0]));
                    sb.Append(word.Substring(1));
                }

                return sb.ToString();
            }
            else
            {
                return char.ToUpper(name[0]) + name.Substring(1);
            }
        }

        public static CodeMemberMethod CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(string name, CodeExpression expression)
        {
            return new CodeMemberMethod
            {
                ReturnType = new CodeTypeReference(typeof(bool)),
                Statements =
                {
                    new CodeMethodReturnStatement()
                    {
                        Expression = new CodeBinaryOperatorExpression()
                        {
                            Left = new CodeBinaryOperatorExpression()
                            {
                                Left = new CodeFieldReferenceExpression()
                                {
                                    FieldName = GetFieldName(name)
                                },
                            Operator = CodeBinaryOperatorType.ValueEquality,
                            Right = expression
                            },
                            Operator = CodeBinaryOperatorType.ValueEquality,
                            Right = new CodePrimitiveExpression(false)
                        }
                    }
                },
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "ShouldSerialize" + name
            };
        }

        public static CodeMemberMethod CreateMethodThatChecksIfTheArrayOfValueOfAMemberIsNotEqualToAnotherExpression(
           string name, CodeExpression expression)
        {
            return new CodeMemberMethod
            {
                ReturnType = new CodeTypeReference(typeof(bool)),
                Statements =
                {
                    new CodeMethodReturnStatement()
                    {
                        Expression = new CodeBinaryOperatorExpression()
                        {
                            Left = new CodeMethodInvokeExpression(
                                new CodeFieldReferenceExpression() {FieldName = GetFieldName(name)},
                                "SequenceEqual",
                                new CodeExpression[] { expression}
                                )
                            ,
                            Operator = CodeBinaryOperatorType.ValueEquality,
                            Right = new CodePrimitiveExpression(false)
                        }
                    }
                },
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "ShouldSerialize" + name
            };
        }
    }
}
