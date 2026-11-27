using System.Text.RegularExpressions;

namespace backend.Services;

public static partial class ItJobFilter
{
    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:Software\s+(?:Engineer|Developer|Architect|Programmer|QA|Quality\s+Assurance|Tester)|(?:Full\s*Stack|Fullstack|Front[\s-]*End|Back[\s-]*End|Web|Mobile|Android|iOS|Game|Application|App|Cloud|DevOps|Platform|Site\s+Reliability|Data|AI|Machine\s+Learning|ML|Security|Cyber\s*Security|Network|System|Systems|Embedded|Firmware|Database)\s+(?:Engineer|Developer|Architect|Programmer|Administrator|Analyst|Specialist|Consultant)|(?:IT|Information\s+Technology)\s+(?:Support|Helpdesk|Help\s*Desk|Infrastructure|Security|Network|System|Systems|Administrator|Admin|Engineer|Developer|Programmer|Analyst|Specialist|Auditor|Consultant|Staff|Technician|Officer)|(?:Staff|Spesialis|Specialist|Admin)\s+IT|Helpdesk\s+IT|Technical\s+Support\s+IT|Sysadmin|System\s+Administrator|Network\s+Administrator|Database\s+Administrator|DBA|SOC\s+Analyst|Blue\s+Team|Red\s+Team|Penetration\s+Tester|Pentester|Cyber\s*Security|Cybersecurity|Information\s+Security|DevOps|DevSecOps|SRE|Programmer|UI\s*/\s*UX(?:\s+(?:Designer|Researcher|Engineer))?|UX\s+(?:Designer|Researcher)|UI\s+Designer|(?:Software|Automation|Manual)\s+Tester|QA\s+(?:Engineer|Automation|Tester|Software)|Quality\s+Assurance\s+(?:Engineer|Software)|Scrum\s+Master|Tech\s+Lead|Technical\s+Lead)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StrongItRolePattern();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:TypeScript|JavaScript|React(?:\.js)?|Next\.js|Vue(?:\.js)?|Angular|Node(?:\.js)?|ASP\.NET|\.NET\s*(?:Core)?|C#|Golang|Python|Django|FastAPI|Spring\s+Boot|Laravel|PHP|PostgreSQL|MySQL|SQL\s+Server|MongoDB|Redis|Docker|Kubernetes|Terraform|CI\s*/\s*CD|REST\s*API|GraphQL|GitHub|GitLab|Selenium|Playwright|Cypress|Appium|Postman|OWASP|SIEM|Firewall|MikroTik|Cisco|TCP/IP|LAN/WAN|Active\s+Directory|Linux\s+Server|AWS|Azure|GCP|Google\s+Cloud|Figma|Wireframe)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TechnicalStackPattern();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:Quality\s+Assurance|QA)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GenericQaPattern();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:software\s+testing|test\s+case|test\s+plan|bug\s+report|regression\s+testing|api\s+testing|automated\s+testing|unit\s+test|integration\s+test|UAT|SDLC|Selenium|Playwright|Cypress|Appium|Postman|JIRA|JMeter|Katalon)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SoftwareQaEvidencePattern();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:Operator\s+Produksi|Quality\s+Control|QC\s+(?:Staff|Inspector|Produksi|Pabrik)|Bisnis\s+Digital|Digital\s+Marketing|Social\s+Media|Content\s+Creator|SOP|ISO\s+\d+|GMP|HACCP|manufaktur|pabrik|line\s+produksi|mesin\s+produksi|pendidikan\s+SMP|lulusan\s+SMP|audit\s+operasional|kualitas\s+operasional|gudang|warehouse|kasir|sales|marketing|customer\s+service|administrasi\s+umum|satpam)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NonItContextPattern();

    public static bool IsRelevant(string title, string cleanContent)
    {
        var normalizedTitle = title ?? string.Empty;
        var normalizedContent = cleanContent ?? string.Empty;
        var combined = $"{normalizedTitle}\n{normalizedContent}";

        var hasStrongRoleInTitle = StrongItRolePattern().IsMatch(normalizedTitle);
        var hasStrongRoleInContent = StrongItRolePattern().IsMatch(normalizedContent);

        if (GenericQaPattern().IsMatch(normalizedTitle) &&
            !hasStrongRoleInTitle &&
            !SoftwareQaEvidencePattern().IsMatch(combined))
        {
            return false;
        }

        if (hasStrongRoleInTitle)
        {
            return true;
        }

        var distinctTechSignals = TechnicalStackPattern()
            .Matches(combined)
            .Select(match => match.Value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();

        var hasNonItContext = NonItContextPattern().IsMatch(combined);

        if (hasStrongRoleInContent)
        {
            if (!hasNonItContext)
            {
                return true;
            }

            return distinctTechSignals >= 2 ||
                SoftwareQaEvidencePattern().IsMatch(combined);
        }

        if (GenericQaPattern().IsMatch(combined) &&
            SoftwareQaEvidencePattern().IsMatch(combined))
        {
            return true;
        }

        return !hasNonItContext && distinctTechSignals >= 3;
    }
}
