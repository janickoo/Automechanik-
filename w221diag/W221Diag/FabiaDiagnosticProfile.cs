using System.Globalization;
using System.Text;
namespace W221Diag;

public enum VehicleMatch { Unknown, Match, Mismatch }

public static class FabiaDiagnosticProfile
{
    public sealed record CaseInput(string Vin, string Year, string Mileage, string Transmission,
        string Symptoms, string Conditions, string Dtcs, string PreviousRepairs, string Measurements);
    public sealed record CaseReport(int SchemaVersion, DateTimeOffset CreatedAtUtc, string Vehicle,
        CaseInput Input, string Procedure, string Workflow, IReadOnlyList<string> MissingInputs,
        VehicleMatch ImportStatus, IReadOnlyList<TexaSessionInspector.EcuScanResult> ImportedEcus);

    public static IReadOnlyList<string> Procedures { get; } = new[]
        { "Vstupná kontrola", "Tlak oleja", "Vynechávanie", "Elektrika / komunikácia" };

    public static VehicleMatch CheckVehicle(TexaSessionInspector.VehicleMetadata? vehicle, string expectedVin)
    {
        return VehicleMatch.Unknown;
    }

    public static CaseReport CreateReport(CaseInput input, string procedure, TexaSessionInspector.SessionInspection? session)
    {
        var missing = new List<string>();
        foreach (var item in new[] { ("VIN", input.Vin), ("Rok", input.Year), ("Nájazd", input.Mileage),
            ("Prevodovka", input.Transmission), ("Symptómy", input.Symptoms), ("Podmienky závady", input.Conditions),
            ("DTC alebo potvrdenie bez DTC", input.Dtcs), ("Predchádzajúce zásahy", input.PreviousRepairs) })
            if (string.IsNullOrWhiteSpace(item.Item2)) missing.Add(item.Item1);
        if (!string.IsNullOrWhiteSpace(input.Vin) && !IsVin(input.Vin)) missing.Add("Platný 17-znakový VIN");
        var match = CheckVehicle(session?.Vehicle, input.Vin);
        return new(1, DateTimeOffset.UtcNow, "Škoda Fabia Combi · CBZA 1.2 TSI", input, procedure,
            GetWorkflow(procedure), missing, match,
            match == VehicleMatch.Match ? session!.EcuResults : Array.Empty<TexaSessionInspector.EcuScanResult>());
    }

    public static string GetWorkflow(string procedure)
    {
        const string common = """
            PRÍPRAVA — Škoda Fabia Combi · CBZA 1.2 TSI
            Doplň VIN, rok, nájazd, prevodovku, symptómy, podmienky a predchádzajúce zásahy.
            V TEXA IDC6 vyber vozidlo podľa VIN a motor CBZA. Ulož identifikáciu RJ, kompletný scan, stav DTC a freeze-frame pred mazaním.
            Názvy parametrov závisia od vozidla a verzie IDC6. Pinouty, poistky a meracie limity over podľa VIN v dielenskej príručke.
            Aplikácia pracuje s offline TEXA session a ručne zapísanými meraniami. Priame spojenie s RJ nie je implementované.

            """;
        return common + (procedure switch
        {
            "Tlak oleja" => """
                KROK 1 — Over hladinu, olej, filter a okolnosti výstrahy. Pri podozrení na skutočne nízky tlak motor nezaťažuj.
                KROK 2 — Mechanickým manometrom zmeraj tlak v predpísanom bode; zapíš teplotu oleja, otáčky, tlak a OEM limit so zdrojom.
                AK tlak nespĺňa predpis: pokračuj kontrolou mazania podľa príručky.
                AK tlak spĺňa predpis v okamihu výstrahy: pokračuj kontrolou správneho tlakového spínača, konektora a obvodu podľa schémy.
                KROK 3 — Zaznamenaj súčasne tlak, otáčky a elektrický stav spínača. Over vedenie a napájanie/kostry príslušných modulov pod záťažou.
                AK stav spínača nezodpovedá tlaku: over jeho typ a spínaciu hranicu.
                AK súhlasí: sleduj signál až po prijímajúcu RJ, prever kontakty a urob wiggle test.
                ĎALŠIE MERANIE — porovnať mechanický tlak a elektrický stav v okamihu výstrahy. Univerzálna tlaková hranica nie je dosadená.
                """,
            "Vynechávanie" => """
                KROK 1 — Ulož DTC, freeze-frame a počítadlá vynechávania po valcoch. Loguj otáčky, záťaž, teplotu, lambda/korekcie zmesi a požadovaný/skutočný tlak paliva, ak sú dostupné.
                KROK 2 — Pri jednom valci over sviečku, zapaľovací výstup, kábel a vstrekovanie. Zámennú skúšku rob iba so samostatným zameniteľným dielom.
                AK sa chyba presunie s dielom: potvrď výsledok opakovaným testom.
                AK zostane: meraj kompresiu/leak-down a prever vstrekovanie.
                KROK 3 — Ak závada vznikla po rozvodoch alebo strete, prioritne over časovanie a tesnosť valcov pred ďalším zaťažovaním.
                AK sú postihnuté viaceré valce: prever spoločné napájanie, palivo, netesnosti sania a časovanie.
                ĎALŠIE MERANIE — porovnať postihnutý valec s ostatnými za rovnakých podmienok. Vysokotlakové palivové vedenie nepovoľuj za chodu.
                """,
            "Elektrika / komunikácia" => """
                KROK 1 — Zmapuj komunikujúce a nekomunikujúce RJ cez TEXA. Ulož identifikáciu aj chyby napájania/komunikácie.
                KROK 2 — Ak nekomunikuje iba jedna RJ, over jej poistky, napájanie a kostry pod záťažou podľa VIN schémy.
                AK napájanie/kostra zlyháva: lokalizuj úbytok napätia.
                AK sú v poriadku: skontroluj kontakty a príslušnú sieť osciloskopom.
                KROK 3 — Ak nekomunikuje skupina RJ, hľadaj spoločné napájanie alebo vetvu siete. Odpor meraj iba na bezpečne vypnutej sieti podľa OEM postupu.
                ĎALŠIE MERANIE — úbytok na kladnom prívode a kostre konkrétnej RJ počas prejavu závady. Univerzálna topológia CAN sa nepredpokladá.
                """,
            "Vstupná kontrola" => """
                KROK 1 — Over identifikáciu vozidla a motora, napätie akumulátora a vizuálny stav konektorov/kvapalín.
                KROK 2 — TEXA: identifikácia RJ, kompletný scan, DTC so stavom a freeze-frame. Kód sám nepotvrdzuje chybný diel.
                KROK 3 — Podľa symptómu vyber tlak oleja, vynechávanie alebo elektriku.
                AK závada vznikla po zásahu: najprv prever rozoberané konektory, kostry, vedenia a časovanie.
                AK je sporadická: loguj súvisiace parametre a merania pri prejave.
                ĎALŠÍ KROK — doplniť chýbajúce údaje, potom zvoliť prvý merací test.
                """,
            _ => throw new ArgumentException("Neznámy diagnostický postup.", nameof(procedure))
        });
    }

    private static bool IsVin(string? value)
    {
        string vin = (value ?? "").Trim().ToUpperInvariant();
        return vin.Length == 17 && vin.All(c => c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' && c != 'I' && c != 'O' && c != 'Q');
    }

    private static string Normalize(string? text)
    {
        var result = new StringBuilder();
        foreach (char c in (text ?? "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) result.Append(char.ToUpperInvariant(c));
        return result.ToString().Trim();
    }
}
