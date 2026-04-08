namespace WebAPI;
using System.Reflection;

public class DTO<Tdto, Torg>
    where Tdto : new()
{
    public static Tdto Convert(Torg org)
    {
        Tdto dto = new Tdto();

        var dtoProps = typeof(Tdto).GetProperties();
        var orgProps = typeof(Torg).GetProperties();

        foreach (var dtoProp in dtoProps)
        {
            var orgProp = Array.Find(orgProps, p => p.Name == dtoProp.Name);

            if (orgProp != null && orgProp.PropertyType == dtoProp.PropertyType)
            {
                var value = orgProp.GetValue(org);
                dtoProp.SetValue(dto, value);
            }
        }

        return dto;
    }
}