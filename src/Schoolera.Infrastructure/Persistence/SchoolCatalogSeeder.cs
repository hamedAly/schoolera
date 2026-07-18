using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence;

public sealed class SchoolCatalogSeeder(
    SchooleraDbContext dbContext,
    ILogger<SchoolCatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running school catalog seed...");

        var taxonomy = await SeedTaxonomiesAsync(cancellationToken);
        var inserted = await SeedSchoolsAsync(taxonomy, cancellationToken);
        inserted += await SeedPhase1StatusSchoolsAsync(taxonomy, cancellationToken);

        if (inserted > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("School catalog seed completed. Schools inserted={Inserted}.", inserted);
    }

    private async Task<TaxonomyContext> SeedTaxonomiesAsync(CancellationToken cancellationToken)
    {
        // Coverage: Egypt country + all 27 governorates are complete taxonomy seed.
        // Cities/districts remain demo-only (Cairo, Giza, Alexandria + sample districts).
        var egypt = await EnsureCountryAsync("EG", "egypt", "مصر", "Egypt", 1, cancellationToken);
        var govCairo = await EnsureGovernorateAsync(egypt, "cairo", "القاهرة", "Cairo", 1, cancellationToken);
        var govGiza = await EnsureGovernorateAsync(egypt, "giza", "الجيزة", "Giza", 2, cancellationToken);
        var govAlexandria = await EnsureGovernorateAsync(egypt, "alexandria", "الإسكندرية", "Alexandria", 3, cancellationToken);

        // Remaining Egyptian governorates (verified ISO-independent slugs). Cities not seeded for these.
        await EnsureGovernorateAsync(egypt, "qalyubia", "القليوبية", "Qalyubia", 4, cancellationToken);
        await EnsureGovernorateAsync(egypt, "port-said", "بورسعيد", "Port Said", 5, cancellationToken);
        await EnsureGovernorateAsync(egypt, "suez", "السويس", "Suez", 6, cancellationToken);
        await EnsureGovernorateAsync(egypt, "dakahlia", "الدقهلية", "Dakahlia", 7, cancellationToken);
        await EnsureGovernorateAsync(egypt, "sharqia", "الشرقية", "Sharqia", 8, cancellationToken);
        await EnsureGovernorateAsync(egypt, "gharbia", "الغربية", "Gharbia", 9, cancellationToken);
        await EnsureGovernorateAsync(egypt, "monufia", "المنوفية", "Monufia", 10, cancellationToken);
        await EnsureGovernorateAsync(egypt, "beheira", "البحيرة", "Beheira", 11, cancellationToken);
        await EnsureGovernorateAsync(egypt, "kafr-el-sheikh", "كفر الشيخ", "Kafr El Sheikh", 12, cancellationToken);
        await EnsureGovernorateAsync(egypt, "damietta", "دمياط", "Damietta", 13, cancellationToken);
        await EnsureGovernorateAsync(egypt, "ismailia", "الإسماعيلية", "Ismailia", 14, cancellationToken);
        await EnsureGovernorateAsync(egypt, "beni-suef", "بني سويف", "Beni Suef", 15, cancellationToken);
        await EnsureGovernorateAsync(egypt, "fayoum", "الفيوم", "Fayoum", 16, cancellationToken);
        await EnsureGovernorateAsync(egypt, "minya", "المنيا", "Minya", 17, cancellationToken);
        await EnsureGovernorateAsync(egypt, "asyut", "أسيوط", "Asyut", 18, cancellationToken);
        await EnsureGovernorateAsync(egypt, "sohag", "سوهاج", "Sohag", 19, cancellationToken);
        await EnsureGovernorateAsync(egypt, "qena", "قنا", "Qena", 20, cancellationToken);
        await EnsureGovernorateAsync(egypt, "luxor", "الأقصر", "Luxor", 21, cancellationToken);
        await EnsureGovernorateAsync(egypt, "aswan", "أسوان", "Aswan", 22, cancellationToken);
        await EnsureGovernorateAsync(egypt, "red-sea", "البحر الأحمر", "Red Sea", 23, cancellationToken);
        await EnsureGovernorateAsync(egypt, "new-valley", "الوادي الجديد", "New Valley", 24, cancellationToken);
        await EnsureGovernorateAsync(egypt, "matrouh", "مطروح", "Matrouh", 25, cancellationToken);
        await EnsureGovernorateAsync(egypt, "north-sinai", "شمال سيناء", "North Sinai", 26, cancellationToken);
        await EnsureGovernorateAsync(egypt, "south-sinai", "جنوب سيناء", "South Sinai", 27, cancellationToken);

        // Map only cities whose governorates are confidently identified (same-name governorates).
        var cairo = await EnsureCityAsync("cairo", "القاهرة", "Cairo", 1, govCairo.Id, cancellationToken);
        var giza = await EnsureCityAsync("giza", "الجيزة", "Giza", 2, govGiza.Id, cancellationToken);
        var alexandria = await EnsureCityAsync(
            "alexandria",
            "الإسكندرية",
            "Alexandria",
            3,
            govAlexandria.Id,
            cancellationToken);

        var nasrCity = await EnsureDistrictAsync(cairo, "nasr-city", "مدينة نصر", "Nasr City", 1, cancellationToken);
        var maadi = await EnsureDistrictAsync(cairo, "maadi", "المعادي", "Maadi", 2, cancellationToken);
        var heliopolis = await EnsureDistrictAsync(cairo, "heliopolis", "مصر الجديدة", "Heliopolis", 3, cancellationToken);
        var dokki = await EnsureDistrictAsync(giza, "dokki", "الدقي", "Dokki", 1, cancellationToken);
        var sixthOctober = await EnsureDistrictAsync(giza, "6th-october", "السادس من أكتوبر", "6th of October", 2, cancellationToken);
        var smouha = await EnsureDistrictAsync(alexandria, "smouha", "سموحة", "Smouha", 1, cancellationToken);
        var miami = await EnsureDistrictAsync(alexandria, "miami", "ميامي", "Miami", 2, cancellationToken);

        var kg = await EnsureStageAsync("kindergarten", "رياض الأطفال", "Kindergarten", 1, cancellationToken);
        var primary = await EnsureStageAsync("primary", "المرحلة الابتدائية", "Primary", 2, cancellationToken);
        var preparatory = await EnsureStageAsync("preparatory", "المرحلة الإعدادية", "Preparatory", 3, cancellationToken);
        var secondary = await EnsureStageAsync("secondary", "المرحلة الثانوية", "Secondary", 4, cancellationToken);

        var kg1 = await EnsureGradeAsync(kg, "kg1", "كي جي 1", "KG1", 1, cancellationToken);
        var kg2 = await EnsureGradeAsync(kg, "kg2", "كي جي 2", "KG2", 2, cancellationToken);
        var grade1 = await EnsureGradeAsync(primary, "grade-1", "الصف الأول", "Grade 1", 1, cancellationToken);
        var grade6 = await EnsureGradeAsync(primary, "grade-6", "الصف السادس", "Grade 6", 6, cancellationToken);
        var grade7 = await EnsureGradeAsync(preparatory, "grade-7", "الصف الأول الإعدادي", "Grade 7", 1, cancellationToken);
        var grade9 = await EnsureGradeAsync(preparatory, "grade-9", "الصف الثالث الإعدادي", "Grade 9", 3, cancellationToken);
        var grade10 = await EnsureGradeAsync(secondary, "grade-10", "الصف الأول الثانوي", "Grade 10", 1, cancellationToken);
        var grade12 = await EnsureGradeAsync(secondary, "grade-12", "الصف الثالث الثانوي", "Grade 12", 3, cancellationToken);

        var national = await EnsureCurriculumAsync(
            "national",
            "المناهج المصرية",
            "Egyptian National",
            "المنهج الوطني المصري",
            "Egyptian national curriculum",
            1,
            cancellationToken);
        var american = await EnsureCurriculumAsync(
            "american",
            "المناهج الأمريكية",
            "American",
            "المنهج الأمريكي",
            "American curriculum",
            2,
            cancellationToken);
        var british = await EnsureCurriculumAsync(
            "british",
            "المناهج البريطانية",
            "British",
            "المنهج البريطاني",
            "British curriculum",
            3,
            cancellationToken);
        var ib = await EnsureCurriculumAsync(
            "ib",
            "البكالوريا الدولية",
            "IB",
            "برنامج البكالوريا الدولية",
            "International Baccalaureate",
            4,
            cancellationToken);

        var library = await EnsureFacilityAsync("library", "مكتبة", "Library", "library", 1, cancellationToken);
        var scienceLab = await EnsureFacilityAsync("science-lab", "معمل علوم", "Science Lab", "science", 2, cancellationToken);
        var sports = await EnsureFacilityAsync("sports", "ملاعب رياضية", "Sports Fields", "sports", 3, cancellationToken);
        var pool = await EnsureFacilityAsync("swimming-pool", "حمام سباحة", "Swimming Pool", "pool", 4, cancellationToken);
        var cafeteria = await EnsureFacilityAsync("cafeteria", "كافتيريا", "Cafeteria", "cafeteria", 5, cancellationToken);
        var playground = await EnsureFacilityAsync("playground", "ملعب أطفال", "Playground", "playground", 6, cancellationToken);
        var computerLab = await EnsureFacilityAsync("computer-lab", "معمل حاسب", "Computer Lab", "computer", 7, cancellationToken);
        var auditorium = await EnsureFacilityAsync("auditorium", "قاعة متعددة الأغراض", "Auditorium", "auditorium", 8, cancellationToken);

        var academicYear = await EnsureAcademicYearAsync(
            "2025-2026",
            "العام الدراسي 2025-2026",
            "Academic Year 2025-2026",
            new DateOnly(2025, 9, 1),
            new DateOnly(2026, 6, 30),
            isCurrent: true,
            cancellationToken);

        return new TaxonomyContext(
            cairo,
            giza,
            alexandria,
            nasrCity,
            maadi,
            heliopolis,
            dokki,
            sixthOctober,
            smouha,
            miami,
            kg,
            primary,
            preparatory,
            secondary,
            kg1,
            kg2,
            grade1,
            grade6,
            grade7,
            grade9,
            grade10,
            grade12,
            national,
            american,
            british,
            ib,
            library,
            scienceLab,
            sports,
            pool,
            cafeteria,
            playground,
            computerLab,
            auditorium,
            academicYear);
    }

    private async Task<int> SeedSchoolsAsync(TaxonomyContext taxonomy, CancellationToken cancellationToken)
    {
        var inserted = 0;

        inserted += await SeedSchoolAsync(
            taxonomy,
            slug: "cairo-international-school",
            nameAr: "مدرسة القاهرة الدولية",
            nameEn: "Cairo International School",
            schoolType: SchoolType.International,
            genderType: GenderType.Mixed,
            logo: "/assets/schools/demo-1.svg",
            cover: "/assets/schools/demo-1.svg",
            foundedYear: 1998,
            studentCount: 1200,
            shortDescriptionAr: "مدرسة دولية رائدة في قلب القاهرة تقدم تعليماً متميزاً.",
            shortDescriptionEn: "A leading international school in Cairo offering outstanding education.",
            branchNameAr: "الفرع الرئيسي - مدينة نصر",
            branchNameEn: "Main Campus - Nasr City",
            branchSlug: "main-nasr-city",
            city: taxonomy.Cairo,
            district: taxonomy.NasrCity,
            latitude: 30.0561m,
            longitude: 31.3300m,
            curricula: [taxonomy.American, taxonomy.British],
            facilities: [taxonomy.Library, taxonomy.ScienceLab, taxonomy.Sports, taxonomy.Pool, taxonomy.ComputerLab],
            stageOfferings:
            [
                (taxonomy.Primary, GenderType.Mixed, [taxonomy.Grade1, taxonomy.Grade6]),
                (taxonomy.Preparatory, GenderType.Mixed, [taxonomy.Grade7, taxonomy.Grade9]),
            ],
            stageFees:
            [
                (taxonomy.Primary, null, 85000m),
                (taxonomy.Preparatory, taxonomy.Grade7, 95000m),
            ],
            imagePaths: ["/assets/schools/demo-1.svg"],
            cancellationToken);

        inserted += await SeedSchoolAsync(
            taxonomy,
            slug: "alexandria-stem-academy",
            nameAr: "أكاديمية الإسكندرية للعلوم والتكنولوجيا",
            nameEn: "Alexandria STEM Academy",
            schoolType: SchoolType.National,
            genderType: GenderType.Boys,
            logo: "/assets/schools/demo-2.svg",
            cover: "/assets/schools/demo-2.svg",
            foundedYear: 2010,
            studentCount: 800,
            shortDescriptionAr: "أكاديمية وطنية متخصصة في العلوم والتكنولوجيا بسموحة.",
            shortDescriptionEn: "A national academy focused on science and technology in Smouha.",
            branchNameAr: "فرع سموحة",
            branchNameEn: "Smouha Campus",
            branchSlug: "smouha-campus",
            city: taxonomy.Alexandria,
            district: taxonomy.Smouha,
            latitude: 31.2156m,
            longitude: 29.9553m,
            curricula: [taxonomy.National],
            facilities: [taxonomy.Library, taxonomy.ScienceLab, taxonomy.ComputerLab, taxonomy.Auditorium],
            stageOfferings:
            [
                (taxonomy.Preparatory, GenderType.Boys, [taxonomy.Grade7, taxonomy.Grade9]),
                (taxonomy.Secondary, GenderType.Boys, [taxonomy.Grade10, taxonomy.Grade12]),
            ],
            stageFees:
            [
                (taxonomy.Preparatory, taxonomy.Grade7, 42000m),
                (taxonomy.Secondary, taxonomy.Grade10, 48000m),
            ],
            imagePaths: ["/assets/schools/demo-2.svg"],
            cancellationToken);

        inserted += await SeedSchoolAsync(
            taxonomy,
            slug: "giza-modern-school",
            nameAr: "مدرسة الجيزة الحديثة",
            nameEn: "Giza Modern School",
            schoolType: SchoolType.Private,
            genderType: GenderType.Mixed,
            logo: "/assets/schools/demo-3.svg",
            cover: "/assets/schools/demo-3.svg",
            foundedYear: 2005,
            studentCount: 650,
            shortDescriptionAr: "مدرسة خاصة حديثة في الدقي بمرافق تعليمية متكاملة.",
            shortDescriptionEn: "A modern private school in Dokki with comprehensive facilities.",
            branchNameAr: "فرع الدقي",
            branchNameEn: "Dokki Campus",
            branchSlug: "dokki-campus",
            city: taxonomy.Giza,
            district: taxonomy.Dokki,
            latitude: 30.0380m,
            longitude: 31.2090m,
            curricula: [taxonomy.National, taxonomy.British],
            facilities: [taxonomy.Library, taxonomy.Sports, taxonomy.Cafeteria, taxonomy.Playground],
            stageOfferings:
            [
                (taxonomy.Kindergarten, GenderType.Mixed, [taxonomy.Kg1, taxonomy.Kg2]),
                (taxonomy.Primary, GenderType.Mixed, [taxonomy.Grade1, taxonomy.Grade6]),
            ],
            stageFees:
            [
                (taxonomy.Kindergarten, taxonomy.Kg1, 35000m),
                (taxonomy.Primary, taxonomy.Grade1, 40000m),
            ],
            imagePaths: ["/assets/schools/demo-3.svg"],
            cancellationToken);

        inserted += await SeedSchoolAsync(
            taxonomy,
            slug: "nile-language-school",
            nameAr: "مدرسة النيل للغات",
            nameEn: "Nile Language School",
            schoolType: SchoolType.Language,
            genderType: GenderType.Girls,
            logo: "/assets/schools/demo-4.svg",
            cover: "/assets/schools/demo-4.svg",
            foundedYear: 1992,
            studentCount: 900,
            shortDescriptionAr: "مدرسة لغات للبنات في مصر الجديدة مع مناهج وطنية وبكالوريا دولية.",
            shortDescriptionEn: "A girls language school in Heliopolis with national and IB programs.",
            branchNameAr: "فرع مصر الجديدة",
            branchNameEn: "Heliopolis Campus",
            branchSlug: "heliopolis-campus",
            city: taxonomy.Cairo,
            district: taxonomy.Heliopolis,
            latitude: 30.0875m,
            longitude: 31.3240m,
            curricula: [taxonomy.National, taxonomy.Ib],
            facilities: [taxonomy.Library, taxonomy.ScienceLab, taxonomy.Auditorium, taxonomy.Cafeteria],
            stageOfferings:
            [
                (taxonomy.Primary, GenderType.Girls, [taxonomy.Grade1, taxonomy.Grade6]),
                (taxonomy.Secondary, GenderType.Girls, [taxonomy.Grade10, taxonomy.Grade12]),
            ],
            stageFees:
            [
                (taxonomy.Primary, taxonomy.Grade1, 55000m),
                (taxonomy.Secondary, taxonomy.Grade10, 72000m),
            ],
            imagePaths: ["/assets/schools/demo-4.svg"],
            cancellationToken);

        return inserted;
    }

    /// <summary>
    /// Minimal Unpublished / Suspended schools for Phase 1 QA (idempotent by slug).
    /// Does not alter published <c>cairo-international-school</c>.
    /// </summary>
    private async Task<int> SeedPhase1StatusSchoolsAsync(
        TaxonomyContext taxonomy,
        CancellationToken cancellationToken)
    {
        var inserted = 0;

        inserted += await SeedMinimalStatusSchoolAsync(
            slug: "demo-unpublished-school",
            nameAr: "مدرسة تجريبية غير منشورة",
            nameEn: "Demo Unpublished School",
            status: SchoolStatus.Unpublished,
            city: taxonomy.Cairo,
            district: taxonomy.Maadi,
            cancellationToken);

        inserted += await SeedMinimalStatusSchoolAsync(
            slug: "demo-suspended-school",
            nameAr: "مدرسة تجريبية موقوفة",
            nameEn: "Demo Suspended School",
            status: SchoolStatus.Suspended,
            city: taxonomy.Giza,
            district: taxonomy.Dokki,
            cancellationToken);

        return inserted;
    }

    private async Task<int> SeedMinimalStatusSchoolAsync(
        string slug,
        string nameAr,
        string nameEn,
        SchoolStatus status,
        City city,
        District district,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Schools.AsNoTracking().AnyAsync(school => school.Slug == slug, cancellationToken))
        {
            logger.LogInformation("Catalog status school skipped (already exists by slug): {Slug}.", slug);
            return 0;
        }

        var school = new School(nameAr, nameEn, slug, SchoolType.National, GenderType.Mixed, status);
        dbContext.Schools.Add(school);

        var branch = new SchoolBranch(
            school.Id,
            "الفرع الرئيسي",
            "Main Campus",
            "main",
            city.Id,
            district.Id,
            isMainBranch: true);
        dbContext.SchoolBranches.Add(branch);

        logger.LogInformation("Catalog status school queued for insert: {Slug} ({Status}).", slug, status);
        return 1;
    }

    private async Task<int> SeedSchoolAsync(
        TaxonomyContext taxonomy,
        string slug,
        string nameAr,
        string nameEn,
        SchoolType schoolType,
        GenderType genderType,
        string logo,
        string cover,
        int foundedYear,
        int studentCount,
        string shortDescriptionAr,
        string shortDescriptionEn,
        string branchNameAr,
        string branchNameEn,
        string branchSlug,
        City city,
        District district,
        decimal latitude,
        decimal longitude,
        Curriculum[] curricula,
        Facility[] facilities,
        (EducationalStage Stage, GenderType Gender, Grade[] Grades)[] stageOfferings,
        (EducationalStage Stage, Grade? Grade, decimal Amount)[] stageFees,
        string[] imagePaths,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Schools.AsNoTracking().AnyAsync(school => school.Slug == slug, cancellationToken))
        {
            logger.LogInformation("Catalog school skipped (already exists by slug): {Slug}.", slug);
            return 0;
        }

        var school = new School(nameAr, nameEn, slug, schoolType, genderType, SchoolStatus.Published);
        SetSchoolDetails(school, logo, cover, foundedYear, studentCount, shortDescriptionAr, shortDescriptionEn);
        dbContext.Schools.Add(school);

        foreach (var curriculum in curricula)
        {
            dbContext.SchoolCurricula.Add(new SchoolCurriculum(school.Id, curriculum.Id));
        }

        foreach (var facility in facilities)
        {
            dbContext.SchoolFacilities.Add(new SchoolFacility(school.Id, facility.Id));
        }

        for (var index = 0; index < imagePaths.Length; index++)
        {
            dbContext.SchoolImages.Add(new SchoolImage(school.Id, imagePaths[index], index + 1));
        }

        var branch = new SchoolBranch(
            school.Id,
            branchNameAr,
            branchNameEn,
            branchSlug,
            city.Id,
            district.Id,
            isMainBranch: true);
        SetBranchDetails(branch, latitude, longitude, district);
        dbContext.SchoolBranches.Add(branch);

        foreach (var (stage, gender, grades) in stageOfferings)
        {
            var stageOffering = new SchoolStageOffering(
                branch.Id,
                stage.Id,
                gender,
                capacity: 200,
                isAdmissionOpen: true);
            dbContext.SchoolStageOfferings.Add(stageOffering);

            foreach (var grade in grades)
            {
                dbContext.SchoolGradeOfferings.Add(new SchoolGradeOffering(stageOffering.Id, grade.Id));
            }
        }

        foreach (var (stage, grade, amount) in stageFees)
        {
            var fee = new TuitionFee(
                branch.Id,
                stage.Id,
                grade?.Id,
                taxonomy.AcademicYear.Id,
                FeeCategory.Tuition,
                "EGP",
                amount);
            fee.Publish();
            dbContext.TuitionFees.Add(fee);
        }

        logger.LogInformation("Catalog school queued for insert: {Slug}.", slug);
        return 1;
    }

    private static void SetSchoolDetails(
        School school,
        string logo,
        string cover,
        int foundedYear,
        int studentCount,
        string shortDescriptionAr,
        string shortDescriptionEn)
    {
        typeof(School).GetProperty(nameof(School.LogoUrl))!
            .SetValue(school, logo);
        typeof(School).GetProperty(nameof(School.CoverUrl))!
            .SetValue(school, cover);
        typeof(School).GetProperty(nameof(School.FoundedYear))!
            .SetValue(school, foundedYear);
        typeof(School).GetProperty(nameof(School.StudentCount))!
            .SetValue(school, studentCount);
        typeof(School).GetProperty(nameof(School.ShortDescriptionAr))!
            .SetValue(school, shortDescriptionAr);
        typeof(School).GetProperty(nameof(School.ShortDescriptionEn))!
            .SetValue(school, shortDescriptionEn);
    }

    private static void SetBranchDetails(
        SchoolBranch branch,
        decimal latitude,
        decimal longitude,
        District district)
    {
        typeof(SchoolBranch).GetProperty(nameof(SchoolBranch.Latitude))!
            .SetValue(branch, latitude);
        typeof(SchoolBranch).GetProperty(nameof(SchoolBranch.Longitude))!
            .SetValue(branch, longitude);
        typeof(SchoolBranch).GetProperty(nameof(SchoolBranch.AddressLineAr))!
            .SetValue(branch, $"شارع رئيسي، {district.NameAr}");
        typeof(SchoolBranch).GetProperty(nameof(SchoolBranch.AddressLineEn))!
            .SetValue(branch, $"Main Street, {district.NameEn ?? district.NameAr}");
    }

    private async Task<Country> EnsureCountryAsync(
        string code,
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Countries
            .FirstOrDefaultAsync(country => country.Code == code, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var country = new Country(code, nameAr, nameEn, slug, sortOrder);
        dbContext.Countries.Add(country);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog country seeded: {Code}.", code);
        return country;
    }

    private async Task<Governorate> EnsureGovernorateAsync(
        Country country,
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Governorates.FirstOrDefaultAsync(
            governorate => governorate.CountryId == country.Id && governorate.Slug == slug,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var governorate = new Governorate(country.Id, nameAr, nameEn, slug, sortOrder);
        dbContext.Governorates.Add(governorate);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog governorate seeded: {Slug}.", slug);
        return governorate;
    }

    private async Task<City> EnsureCityAsync(
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        Guid? governorateId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Cities.FirstOrDefaultAsync(city => city.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            // Safe backfill only when unmapped and governorate is verified for this slug.
            if (existing.GovernorateId is null && governorateId is { } verifiedGovernorateId)
            {
                existing.AssignGovernorate(verifiedGovernorateId);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Catalog city governorate backfilled: {Slug}.", slug);
            }

            return existing;
        }

        var city = new City(nameAr, nameEn, slug, sortOrder, governorateId);
        dbContext.Cities.Add(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog city seeded: {Slug}.", slug);
        return city;
    }

    private async Task<District> EnsureDistrictAsync(
        City city,
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Districts
            .FirstOrDefaultAsync(district => district.CityId == city.Id && district.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var district = new District(city.Id, nameAr, nameEn, slug, sortOrder);
        dbContext.Districts.Add(district);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog district seeded: {Slug}.", slug);
        return district;
    }

    private async Task<EducationalStage> EnsureStageAsync(
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.EducationalStages
            .FirstOrDefaultAsync(stage => stage.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var stage = new EducationalStage(nameAr, nameEn, slug, sortOrder);
        dbContext.EducationalStages.Add(stage);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog educational stage seeded: {Slug}.", slug);
        return stage;
    }

    private async Task<Grade> EnsureGradeAsync(
        EducationalStage stage,
        string slug,
        string nameAr,
        string? nameEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Grades
            .FirstOrDefaultAsync(
                grade => grade.EducationalStageId == stage.Id && grade.Slug == slug,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var grade = new Grade(stage.Id, nameAr, nameEn, slug, sortOrder);
        dbContext.Grades.Add(grade);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog grade seeded: {Slug}.", slug);
        return grade;
    }

    private async Task<Curriculum> EnsureCurriculumAsync(
        string slug,
        string nameAr,
        string? nameEn,
        string? descriptionAr,
        string? descriptionEn,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Curricula
            .FirstOrDefaultAsync(curriculum => curriculum.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var curriculum = new Curriculum(nameAr, nameEn, slug, sortOrder);
        typeof(Curriculum).GetProperty(nameof(Curriculum.DescriptionAr))!
            .SetValue(curriculum, descriptionAr);
        typeof(Curriculum).GetProperty(nameof(Curriculum.DescriptionEn))!
            .SetValue(curriculum, descriptionEn);
        dbContext.Curricula.Add(curriculum);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog curriculum seeded: {Slug}.", slug);
        return curriculum;
    }

    private async Task<Facility> EnsureFacilityAsync(
        string slug,
        string nameAr,
        string? nameEn,
        string iconKey,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Facilities
            .FirstOrDefaultAsync(facility => facility.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var facility = new Facility(nameAr, nameEn, slug, sortOrder, iconKey);
        dbContext.Facilities.Add(facility);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog facility seeded: {Slug}.", slug);
        return facility;
    }

    private async Task<AcademicYear> EnsureAcademicYearAsync(
        string slug,
        string nameAr,
        string? nameEn,
        DateOnly startDate,
        DateOnly endDate,
        bool isCurrent,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AcademicYears
            .FirstOrDefaultAsync(year => year.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var academicYear = new AcademicYear(nameAr, nameEn, slug, startDate, endDate, isCurrent);
        dbContext.AcademicYears.Add(academicYear);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog academic year seeded: {Slug}.", slug);
        return academicYear;
    }

    private sealed record TaxonomyContext(
        City Cairo,
        City Giza,
        City Alexandria,
        District NasrCity,
        District Maadi,
        District Heliopolis,
        District Dokki,
        District SixthOctober,
        District Smouha,
        District Miami,
        EducationalStage Kindergarten,
        EducationalStage Primary,
        EducationalStage Preparatory,
        EducationalStage Secondary,
        Grade Kg1,
        Grade Kg2,
        Grade Grade1,
        Grade Grade6,
        Grade Grade7,
        Grade Grade9,
        Grade Grade10,
        Grade Grade12,
        Curriculum National,
        Curriculum American,
        Curriculum British,
        Curriculum Ib,
        Facility Library,
        Facility ScienceLab,
        Facility Sports,
        Facility Pool,
        Facility Cafeteria,
        Facility Playground,
        Facility ComputerLab,
        Facility Auditorium,
        AcademicYear AcademicYear);
}
