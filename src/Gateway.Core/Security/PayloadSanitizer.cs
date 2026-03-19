using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Gateway.Core.Security;

public class PayloadSanitizer
{
    /// <summary>
    /// Lexes incoming string telemetry. 
    /// If the payload contains executable SQL keywords outside of string literals, it is flagged as malicious.
    /// </summary>
    public bool IsSafeTelemetry(string rawPayload)
    {
        // 1. Empty payloads aren't SQL injections. 
        if (string.IsNullOrWhiteSpace(rawPayload)) return true;

        var parser = new TSql160Parser(initialQuotedIdentifiers: false);
        using var reader = new StringReader(rawPayload);

        var scriptFragment = parser.Parse(reader, out IList<ParseError> errors);

        // 2. Inspect the Lexer's Token Stream, NOT the AST errors.
        // Even if the JSON '{' causes a SQL syntax error, the Lexer still tokenizes the entire string.
        if (scriptFragment?.ScriptTokenStream != null)
        {
            // Define the keywords that have no business being in our raw JSON telemetry
            var dangerousTokens = new HashSet<TSqlTokenType>
            {
                TSqlTokenType.Select, TSqlTokenType.Insert, TSqlTokenType.Update,
                TSqlTokenType.Delete, TSqlTokenType.Drop, TSqlTokenType.Union,
                TSqlTokenType.Alter, TSqlTokenType.Create, TSqlTokenType.Truncate,
                TSqlTokenType.Exec, TSqlTokenType.Execute
            };

            bool containsMaliciousKeywords = scriptFragment.ScriptTokenStream
                .Any(token => dangerousTokens.Contains(token.TokenType));

            return !containsMaliciousKeywords;
        }

        return true;
    }
}