using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Refugio.Infrastructure.Mappings;

/// <summary>DateOnly gravado como DATE do Oracle (o provider, por padrão, gravaria como texto).</summary>
public sealed class DataOracleConverter() : ValueConverter<DateOnly, DateTime>(
    data => data.ToDateTime(TimeOnly.MinValue),
    dataHora => DateOnly.FromDateTime(dataHora));

/// <summary>TimeOnly gravado como VARCHAR2(5) "HH:mm": legível em SQL e ordena certo como texto.</summary>
public sealed class HoraOracleConverter() : ValueConverter<TimeOnly, string>(
    hora => hora.ToString("HH:mm", CultureInfo.InvariantCulture),
    texto => TimeOnly.ParseExact(texto, "HH:mm", CultureInfo.InvariantCulture));
