using CVGenerator.Models;
using Serilog;

namespace CVGenerator.Services;

public class ValidationService
{
    public List<string> Validate(CVData data)
    {
        var errors = new List<string>();

        if (data == null)
        {
            errors.Add("البيانات فارغة");
            return errors;
        }

        ValidatePersonalInfo(data.PersonalInfo, errors);
        ValidateEducation(data.Education, errors);
        ValidateExperience(data.Experience, errors);
        ValidateSkills(data.Skills, errors);
        ValidateLanguages(data.Languages, errors);

        foreach (var error in errors)
        {
            Log.Warning("Validation warning: {Error}", error);
        }

        return errors;
    }

    private static void ValidatePersonalInfo(PersonalInfo pi, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(pi.FullName))
            errors.Add("الاسم الكامل مطلوب");

        if (string.IsNullOrWhiteSpace(pi.Email))
            errors.Add("البريد الإلكتروني مطلوب");
        else if (!pi.Email.Contains('@'))
            errors.Add("البريد الإلكتروني غير صالح");

        if (string.IsNullOrWhiteSpace(pi.PhonePrimary))
            errors.Add("رقم الهاتف الأساسي مطلوب");
    }

    private static void ValidateEducation(List<Education> education, List<string> errors)
    {
        foreach (var edu in education)
        {
            if (string.IsNullOrWhiteSpace(edu.Degree))
                errors.Add("يوجد حقل تعليم بدون شهادة");

            if (string.IsNullOrWhiteSpace(edu.Institution))
                errors.Add($"شهادة '{edu.Degree}' بدون مؤسسة");
        }
    }

    private static void ValidateExperience(List<WorkExperience> experience, List<string> errors)
    {
        foreach (var exp in experience)
        {
            if (string.IsNullOrWhiteSpace(exp.Company))
                errors.Add("يوجد حقل خبرة بدون اسم شركة");

            if (string.IsNullOrWhiteSpace(exp.Position))
                errors.Add($"خبرة في '{exp.Company}' بدون منصب");
        }
    }

    private static void ValidateSkills(List<Skill> skills, List<string> errors)
    {
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
                errors.Add("يوجد مهارة بدون اسم");
        }
    }

    private static void ValidateLanguages(List<Language> languages, List<string> errors)
    {
        foreach (var lang in languages)
        {
            if (string.IsNullOrWhiteSpace(lang.Name))
                errors.Add("يوجد لغة بدون اسم");
        }
    }
}
