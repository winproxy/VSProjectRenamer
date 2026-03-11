namespace VSProjectRenamer.Core;

/// <summary>
/// All 12 naming-convention variants derived from a single PascalCase input.
/// </summary>
/// <param name="PascalCase">e.g. <c>MyProjectName</c></param>
/// <param name="CamelCase">e.g. <c>myProjectName</c></param>
/// <param name="SnakeCase">e.g. <c>my_project_name</c></param>
/// <param name="KebabCase">e.g. <c>my-project-name</c></param>
/// <param name="UpperSnakeCase">e.g. <c>MY_PROJECT_NAME</c></param>
/// <param name="DotCase">e.g. <c>my.project.name</c></param>
/// <param name="TitleCase">e.g. <c>My Project Name</c></param>
/// <param name="UpperFlatCase">e.g. <c>MYPROJECTNAME</c></param>
/// <param name="LowerFlatCase">e.g. <c>myprojectname</c></param>
/// <param name="Acronym">e.g. <c>MPN</c></param>
/// <param name="LowerSpaceCase">e.g. <c>my project name</c></param>
/// <param name="UpperSpaceCase">e.g. <c>MY PROJECT NAME</c></param>
public sealed record NameVariants(
    string PascalCase,
    string CamelCase,
    string SnakeCase,
    string KebabCase,
    string UpperSnakeCase,
    string DotCase,
    string TitleCase,
    string UpperFlatCase,
    string LowerFlatCase,
    string Acronym,
    string LowerSpaceCase,
    string UpperSpaceCase)
{
    /// <summary>Returns all 12 variants as an enumerable, deduplicated.</summary>
    public IEnumerable<string> All()
    {
        yield return PascalCase;
        yield return CamelCase;
        yield return SnakeCase;
        yield return KebabCase;
        yield return UpperSnakeCase;
        yield return DotCase;
        yield return TitleCase;
        yield return UpperFlatCase;
        yield return LowerFlatCase;
        yield return Acronym;
        yield return LowerSpaceCase;
        yield return UpperSpaceCase;
    }
}
