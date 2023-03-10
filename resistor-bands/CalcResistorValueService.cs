using System.Collections.Concurrent;

class CalcResistorValueService
{
    public IResult CalcResistorValue(ConcurrentDictionary<string, ColorDetails> colors, ResistorBands resistorBands)
    {
        if (!colors.TryGetValue(resistorBands.FirstBand, out ColorDetails? firstColorDetails))
        {
            return Results.BadRequest();
        }
        if (!colors.TryGetValue(resistorBands.SecondBand, out ColorDetails? secColorDetails))
        {
            return Results.BadRequest();
        }

        var firstBandValue = firstColorDetails.Value;
        var secBandValue = secColorDetails.Value;
        var bandsAdded = firstBandValue * 10 + secBandValue;

        if (resistorBands.ThirdBand != null)
        {
            if (!colors.TryGetValue(resistorBands.ThirdBand, out ColorDetails? thirdColorDetails))
            {
                return Results.BadRequest();
            }
            var thirdBandValue = thirdColorDetails.Value;
            bandsAdded = bandsAdded * 10 + thirdBandValue;
        }

        if (!colors.TryGetValue(resistorBands.Multiplier, out ColorDetails? multColorDetails))
        {
            return Results.BadRequest();
        }
        if (!colors.TryGetValue(resistorBands.Multiplier, out ColorDetails? toleranceColorDetails))
        {
            return Results.BadRequest();
        }

        var multiplier = multColorDetails.Multiplier;
        var resistorValue = bandsAdded * multiplier;
        var tolerance = toleranceColorDetails.Tolerance;

        return Results.Ok(new ResistorValue(resistorValue, tolerance));
    }
}