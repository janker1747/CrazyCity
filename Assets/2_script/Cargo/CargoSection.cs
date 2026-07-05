using System.Collections.Generic;

public abstract class CargoSection
{
    private readonly List<ActiveCargo> cargos = new();

    public IReadOnlyList<ActiveCargo> Cargos => cargos;
    public int Count => cargos.Count;

    public virtual void AddCargo(ActiveCargo cargo)
    {
        if (cargo == null || cargos.Contains(cargo))
            return;

        cargos.Add(cargo);
    }

    public virtual bool RemoveCargo(ActiveCargo cargo)
    {
        if (cargo == null)
            return false;

        return cargos.Remove(cargo);
    }

    public bool Contains(ActiveCargo cargo)
    {
        return cargo != null && cargos.Contains(cargo);
    }


    protected List<ActiveCargo> CreateSnapshot()
    {
        return new List<ActiveCargo>(cargos);
    }
}
