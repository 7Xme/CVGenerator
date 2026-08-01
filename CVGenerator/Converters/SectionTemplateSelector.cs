using System.Windows;
using System.Windows.Controls;
using CVGenerator.Models.SectionModels;

namespace CVGenerator.Converters;

public class SectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ObjectiveTemplate { get; set; }
    public DataTemplate? WorkExperienceTemplate { get; set; }
    public DataTemplate? EducationTemplate { get; set; }
    public DataTemplate? SkillsTemplate { get; set; }
    public DataTemplate? LanguagesTemplate { get; set; }
    public DataTemplate? InterestsTemplate { get; set; }
    public DataTemplate? ReferencesTemplate { get; set; }
    public DataTemplate? CoursesTemplate { get; set; }
    public DataTemplate? AchievementsTemplate { get; set; }
    public DataTemplate? PublicationsTemplate { get; set; }
    public DataTemplate? CustomTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            ObjectiveSection => ObjectiveTemplate,
            WorkExperienceSection => WorkExperienceTemplate,
            EducationSection => EducationTemplate,
            SkillsSection => SkillsTemplate,
            LanguagesSection => LanguagesTemplate,
            InterestsSection => InterestsTemplate,
            ReferencesSection => ReferencesTemplate,
            CoursesSection => CoursesTemplate,
            AchievementsSection => AchievementsTemplate,
            PublicationsSection => PublicationsTemplate,
            CustomSection => CustomTemplate,
            _ => null
        };
    }
}
