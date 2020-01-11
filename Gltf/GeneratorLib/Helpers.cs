using System.CodeDom;
using System.Text;

namespace GeneratorLib
{
    public static class Helpers
    {
        public static string GetFieldName(string name)
        {
            return "_" + name[..1].ToLower() + name[1..];
        }

        public static string ToPascalCase(string name)
        {
            if (name.Contains(' ') || name.Contains('_') || name == name.ToUpper())
            {
                var words = name.ToLower().Split(' ', '_');

                StringBuilder sb = new StringBuilder();

                foreach (string word in words)
                {
                    sb.Append(word[..1].ToUpper());
                    sb.Append(word[1..]);
                }

                return sb.ToString();
            }
            else
            {
                return name[..1].ToUpper() + name[1..];
            }
        }

        public static CodeMemberMethod CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(
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
