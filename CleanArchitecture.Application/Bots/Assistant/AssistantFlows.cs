using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Assistant
{
    // ======================================================================
    // ربات «دستیار ناراگرام». هر بخش یک جریان است؛ ادمین در پنل منوی ربات را می‌چیند و هر
    // گزینه را به یکی از این‌ها وصل می‌کند. جریان «آب‌وهوا» و «اوقات شرعی» تنظیم «شهر» دارند:
    // «شهر کاربر» از پروفایل می‌خواند، «انتخاب شهر» می‌پرسد.
    // ======================================================================

    internal static class AssistantFlowKit
    {
        public const string MyCity = "شهر کاربر";
        public const string AskCity = "انتخاب شهر";

        /// <summary>تاریخ امروز ایران، ثابت در بازپخش جریان.</summary>
        public static async Task<DateOnly> TodayAsync(BotFlowContext c)
            => DateOnly.ParseExact(
                await c.CallAsync("today", _ => Task.FromResult(IranTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
                "yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static async Task<AssistantCity> CityAsync(BotFlowContext c, bool forceAsk, string question = "کدام شهر؟ نام شهر را بنویسید یا انتخاب کنید.")
        {
            if (!forceAsk && AssistantCities.Find(c.User.City) is { } mine)
                return mine;

            var prompt = forceAsk || string.IsNullOrWhiteSpace(c.User.City)
                ? question
                : $"شهر شما در پروفایل «{c.User.City}» است که آن را نمی‌شناسم. {question}";

            var answer = await c.AskTextAsync(
                prompt,
                maxLength: 40,
                validate: s => AssistantCities.Find(s) is null
                    ? "این شهر را نمی‌شناسم؛ نام یکی از مراکز استان یا شهرهای بزرگ را بنویسید."
                    : null,
                suggestions: AssistantCities.Popular,
                placeholder: "نام شهر…");
            return AssistantCities.Find(answer)!;
        }
    }

    [BotFlow("assistant.weather", "آب‌وهوا", Description = "هوای الان و سه روز آینده‌ی شهر کاربر یا شهر انتخابی (MET Norway).")]
    [FlowParam("city", "شهر", Options = new[] { AssistantFlowKit.MyCity, AssistantFlowKit.AskCity }, Required = false)]
    public sealed class AssistantWeatherFlow(IWeatherSource weather) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var city = await AssistantFlowKit.CityAsync(c, c.Param("city", AssistantFlowKit.MyCity) == AssistantFlowKit.AskCity);
            var report = await c.CallAsync("weather", ct => weather.GetAsync(city, ct));
            await c.SayAsync(AssistantTexts.Weather(report), menuButton: true);
        }
    }

    [BotFlow("assistant.prices", "قیمت ارز و طلا", Description = "دلار، یورو، درهم، سکه، طلا، تتر و بیت‌کوین به تومان.")]
    public sealed class AssistantPricesFlow(IPriceSource prices) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var snapshot = await c.CallAsync("prices", ct => prices.GetAsync(ct));
            if (!snapshot.Ok)
            {
                await c.SayAsync(AssistantTexts.PricesHeader(snapshot), menuButton: true);
                return;
            }
            await c.SayAsync(AssistantTexts.PricesHeader(snapshot), menuButton: true, card: AssistantTexts.PriceRows(snapshot));
        }
    }

    [BotFlow("assistant.prayer", "اوقات شرعی", Description = "اذان صبح، ظهر و مغرب، طلوع و غروب؛ محاسبه بدون اینترنت.")]
    [FlowParam("city", "شهر", Options = new[] { AssistantFlowKit.MyCity, AssistantFlowKit.AskCity }, Required = false)]
    public sealed class AssistantPrayerFlow(PersianCalendarService calendar) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var city = await AssistantFlowKit.CityAsync(c, c.Param("city", AssistantFlowKit.MyCity) == AssistantFlowKit.AskCity);
            var today = await AssistantFlowKit.TodayAsync(c);
            var times = PrayerTimes.Compute(city, today);
            var day = calendar.Get(today);
            await c.SayAsync(
                $"🕌 اوقات شرعی {city.Name}، {day.Weekday} {day.Persian}\n(به افق {city.Name}؛ ممکن است یکی‌دو دقیقه با تقویم رسمی فرق کند)",
                menuButton: true,
                card: AssistantTexts.PrayerRows(times));
        }
    }

    [BotFlow("assistant.today", "تقویم امروز", Description = "تاریخ شمسی، قمری و میلادی، مناسبت‌ها و تعطیلی بعدی.")]
    public sealed class AssistantTodayFlow(PersianCalendarService calendar) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var today = await AssistantFlowKit.TodayAsync(c);
            await c.SayAsync(AssistantTexts.Today(calendar.Get(today)), menuButton: true);
        }
    }

    [BotFlow("assistant.hafez", "فال حافظ", Description = "یک غزل تصادفی از دیوان حافظ با تعبیر.")]
    public sealed class AssistantHafezFlow : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            await c.ChooseAsync(
                "ای حافظ شیرازی! تو محرم هر رازی...\nنیت کنید، بعد دکمه را بزنید.",
                new[] { new BotOption("go", "🌹 فال بگیر", "primary") });

            var number = await c.CallAsync("ghazal", _ => Task.FromResult(HafezBook.RandomNumber()));
            var ghazal = HafezBook.Get(number);
            if (ghazal is null)
            {
                await c.SayAsync(AssistantTexts.Unavailable, menuButton: true);
                return;
            }

            await c.SayAsync($"📜 غزل {AssistantFormat.Persian(number.ToString(CultureInfo.InvariantCulture))}\n\n{HafezBook.Text(ghazal)}");
            await c.SayAsync("🔮 تعبیر: " + ghazal.Interpretation, menuButton: true);
        }
    }

    [BotFlow("assistant.digest", "خلاصه‌ی صبحگاهی", Description = "اشتراک یا لغو پیام هر روز صبح: تاریخ و مناسبت، هوای شهر، قیمت‌ها و اوقات شرعی.")]
    public sealed class AssistantDigestFlow(IAssistantDigestService digest) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var sub = await c.CallAsync("sub", ct => digest.GetAsync(c.BotId, c.User.Id, ct));
            var hour = AssistantFormat.Persian(digest.Hour.ToString());

            if (sub.Subscribed)
            {
                var action = await c.ChooseAsync(
                    $"شما مشترک خلاصه‌ی صبحگاهی هستید؛ هر روز ساعت {hour} صبح برای شهر {sub.City} می‌رسد.",
                    new[]
                    {
                        new BotOption("city", "🏙 تغییر شهر"),
                        new BotOption("off", "🔕 لغو اشتراک"),
                        new BotOption("keep", "باشد، همین خوب است"),
                    });

                if (action == "off")
                {
                    await c.CallAsync("off", ct => digest.UnsubscribeAsync(c.BotId, c.User.Id, ct));
                    await c.SayAsync("اشتراک لغو شد. هر وقت خواستید از همین‌جا دوباره فعالش کنید.", menuButton: true);
                }
                else if (action == "city")
                {
                    var city = await AssistantFlowKit.CityAsync(c, forceAsk: true, "خلاصه برای کدام شهر باشد؟");
                    await c.CallAsync("set-city", ct => digest.SubscribeAsync(c.BotId, c.User.Id, city.Name, ct));
                    await c.SayAsync($"از فردا خلاصه برای {city.Name} می‌آید.", menuButton: true);
                }
                else
                {
                    await c.SayAsync("👌", menuButton: true);
                }
                return;
            }

            var first = await AssistantFlowKit.CityAsync(c, forceAsk: false, "خلاصه برای کدام شهر باشد؟");
            var preview = await c.CallAsync("preview", ct => digest.BuildAsync(c.User.FirstName, first.Name, ct));
            await c.SayAsync("نمونه‌ی امروز:\n\n" + preview);

            var ok = await c.ConfirmAsync($"هر روز ساعت {hour} صبح همین را برایتان بفرستم؟", yes: "بله، مشترک شو", no: "نه، ممنون");
            if (!ok)
            {
                await c.SayAsync("باشد. هر وقت خواستید از همین‌جا فعالش کنید.", menuButton: true);
                return;
            }

            await c.CallAsync("on", ct => digest.SubscribeAsync(c.BotId, c.User.Id, first.Name, ct));
            await c.SayAsync($"✅ مشترک شدید؛ از فردا ساعت {hour} صبح.", menuButton: true);
        }
    }
}
