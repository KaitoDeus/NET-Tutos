using NET_Tutos.Models.Entities;

namespace NET_Tutos.Services;

public static class QuizTranslations
{
    public record QuizTransItem(string QuestionEn, string OptionAEn, string OptionBEn, string OptionCEn, string OptionDEn, string ExplanationEn);

    private static readonly Dictionary<int, QuizTransItem> ById = new()
    {
        [1] = new(
            "Which component in .NET architecture is responsible for automatic memory management (Garbage Collection) and compiling Intermediate Language (IL) into machine code?",
            "BCL (Base Class Library)",
            "CLR (Common Language Runtime)",
            "MSBuild Tool",
            "SDK CLI",
            "CLR (Common Language Runtime) is the virtual execution environment of .NET, managing Garbage Collection, JIT compilation, and type safety."
        ),
        [2] = new(
            "Which command in the .NET CLI is used to execute a .NET project directly from the command line?",
            "dotnet build",
            "dotnet start",
            "dotnet run",
            "dotnet execute",
            "The 'dotnet run' command builds (if changes detected) and immediately launches the project."
        ),
        [3] = new(
            "In C#, which data type is recommended for financial and monetary calculations to avoid floating-point errors?",
            "float",
            "double",
            "decimal",
            "long",
            "The 'decimal' type provides 128-bit high precision without binary floating-point rounding errors, making it ideal for currency and finance."
        ),
        [4] = new(
            "Why should you use StringBuilder instead of the '+' operator for string concatenation in large loops?",
            "Because '+' produces syntax errors in C#",
            "Because strings in C# are immutable; repeated concatenation creates excessive objects on the heap",
            "Because StringBuilder automatically encodes strings to UTF-32",
            "Because strings only support a maximum length of 256 characters",
            "Strings in C# are immutable. Each '+' concatenation creates a new allocation on the Heap. StringBuilder uses a mutable internal buffer, drastically reducing memory allocations."
        ),
        [5] = new(
            "In C#, which keyword is used in a parent class method to permit child classes to override its behavior?",
            "static",
            "virtual",
            "sealed",
            "const",
            "The 'virtual' keyword marks a base class method as overrideable by derived classes using the 'override' keyword."
        ),
        [6] = new(
            "In LINQ, which of the following methods triggers Immediate Execution of the query?",
            "Where()",
            "Select()",
            "OrderBy()",
            "ToList()",
            "Where, Select, and OrderBy use Deferred Execution. Calling ToList(), ToArray(), FirstOrDefault(), or iterating via foreach triggers immediate query execution."
        ),
        [7] = new(
            "In EF Core, which method optimizes read-only query performance by disabling change tracking?",
            "AsReadOnly()",
            "AsNoTracking()",
            "WithoutTracking()",
            "DisableTracker()",
            "AsNoTracking() instructs EF Core not to track entity modifications in DbContext, significantly saving memory and CPU for read-only queries."
        ),
        [8] = new(
            "In ASP.NET Core Middleware pipeline, what is the correct ordering between Authentication and Authorization?",
            "UseAuthorization() must precede UseAuthentication()",
            "UseAuthentication() must precede UseAuthorization()",
            "These middlewares can be placed in any order",
            "No need to call UseAuthentication() if UseAuthorization() is present",
            "You must first authenticate who the user is (Authentication) before evaluating what permissions they have (Authorization)."
        ),
        [9] = new(
            "In ASP.NET Core, EF Core's DbContext is typically registered with which service lifetime?",
            "Transient",
            "Scoped",
            "Singleton",
            "Static",
            "DbContext is recommended to be registered as Scoped so that each HTTP request receives its own DbContext instance, preventing concurrency conflicts across requests."
        )
    };

    public static string GetQuestionText(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.QuestionEn;
        return q.Question ?? string.Empty;
    }

    public static string GetOptionA(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionAEn;
        return q.OptionA ?? string.Empty;
    }

    public static string GetOptionB(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionBEn;
        return q.OptionB ?? string.Empty;
    }

    public static string GetOptionC(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionCEn;
        return q.OptionC ?? string.Empty;
    }

    public static string GetOptionD(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionDEn;
        return q.OptionD ?? string.Empty;
    }

    public static string GetExplanation(this QuizQuestion? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.ExplanationEn;
        return q.Explanation ?? string.Empty;
    }

    public static string GetQuestionText(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.QuestionEn;
        return q.Question ?? string.Empty;
    }

    public static string GetOptionA(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.OptionAEn;
        return q.OptionA ?? string.Empty;
    }

    public static string GetOptionB(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.OptionBEn;
        return q.OptionB ?? string.Empty;
    }

    public static string GetOptionC(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.OptionCEn;
        return q.OptionC ?? string.Empty;
    }

    public static string GetOptionD(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.OptionDEn;
        return q.OptionD ?? string.Empty;
    }

    public static string GetExplanation(this NET_Tutos.Models.ViewModels.QuizItemViewModel? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.QuestionId, out var item)) return item.ExplanationEn;
        return q.Explanation ?? string.Empty;
    }

    public static string GetQuestionText(this NET_Tutos.Models.ViewModels.ExamQuestionItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.QuestionEn;
        return q.Question ?? string.Empty;
    }

    public static string GetOptionA(this NET_Tutos.Models.ViewModels.ExamQuestionItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionAEn;
        return q.OptionA ?? string.Empty;
    }

    public static string GetOptionB(this NET_Tutos.Models.ViewModels.ExamQuestionItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionBEn;
        return q.OptionB ?? string.Empty;
    }

    public static string GetOptionC(this NET_Tutos.Models.ViewModels.ExamQuestionItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionCEn;
        return q.OptionC ?? string.Empty;
    }

    public static string GetOptionD(this NET_Tutos.Models.ViewModels.ExamQuestionItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionDEn;
        return q.OptionD ?? string.Empty;
    }

    public static string GetQuestionText(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.QuestionEn;
        return q.Question ?? string.Empty;
    }

    public static string GetOptionA(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionAEn;
        return q.OptionA ?? string.Empty;
    }

    public static string GetOptionB(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionBEn;
        return q.OptionB ?? string.Empty;
    }

    public static string GetOptionC(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionCEn;
        return q.OptionC ?? string.Empty;
    }

    public static string GetOptionD(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.OptionDEn;
        return q.OptionD ?? string.Empty;
    }

    public static string GetExplanation(this NET_Tutos.Models.ViewModels.ExamQuestionResultItem? q, bool isEn)
    {
        if (q == null) return string.Empty;
        if (isEn && ById.TryGetValue(q.Id, out var item)) return item.ExplanationEn;
        return q.Explanation ?? string.Empty;
    }
}