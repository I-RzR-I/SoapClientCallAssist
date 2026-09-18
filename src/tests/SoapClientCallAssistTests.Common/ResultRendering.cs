using RzR.ResultMessage.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SoapClientCallAssistTests.Common;

public static class ResultRendering
{
    public static string Describe(IResult result)
    {
        if (result.Messages is null || result.Messages.Count == 0)
            return "<no messages>";

        return string.Join(
            " | ",
            result.Messages.Select(message =>
                $"key=[{message.Key ?? "<null>"}] info=[{message.Message?.Info ?? "<null>"}]"));
    }

    public static string Flat(IResult result)
    {
        if (result?.Messages is null || result.Messages.Count == 0)
            return "<no messages>";

        var values = new List<string>();

        foreach (var message in result.Messages)
        {
            values.Add(message.Key ?? "<null key>");
            values.Add(message.Message?.Info ?? "<null info>");
            values.Add(message.LogTraceId ?? string.Empty);

            if (message.Message?.Details is not null)
                foreach (var detail in message.Message.Details)
                    values.Add(Convert.ToString(detail, CultureInfo.InvariantCulture) ?? string.Empty);

            if (message.RelatedObjects is not null)
                foreach (var related in message.RelatedObjects)
                    values.Add(Convert.ToString(related, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return string.Join(" | ", values);
    }

    public static string FirstKey(IResult result)
        => result.Messages.FirstOrDefault()?.Key ?? "<no messages>";
}