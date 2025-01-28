using System.Collections.Generic;
using System.Linq;

public class Equipment
{
    public string Name { get; set; }
    public int PowerUsage { get; set; }
    public int Mass { get; set; }
    public List<Bouns> Bonuses { get; set; }
}

public class PlayerCharacteristics
{
}

public class Hull
{
    public string Type { get; set; }
    public int BaseHp { get; set; }
    public int BaseShield { get; set; }
    public int BaseCapacity { get; set; }
    public int BaseMass { get; set; }
    public int BaseMoveRange { get; set; }
    public int Size { get; set; }
    
    public List<Bouns> Bonuses { get; set; }
}

public class Bouns
{
    public string Description { get; set; }
    public float Value { get; set; }
}

struct Ship
{
    Hull hull;
    List<Equipment> equipment;
}

public class PlayerModel
{
    private PlayerCharacteristics _characteristics;
    private List<Ship> _ships;

    private string _size; // Размер корабля, 1-2
    private int _hp; // Прочность корабля, 100-10000
    private int _shield; // Прочность щита, 0-10000
    private int _capacity; // Емкость аккамулятора, 100-1000, кратно 100
    private int _mass; // Масса корабля, 1?-1000
    private int _moveRange; // Дальность перемещения корабля, 1-5
    private List<Bouns> _bouns; // различные бонусы и штрафы. Например +15% к урону лазерным оружием и тп
    
    public PlayerModel(Hull hull, PlayerCharacteristics characteristics, List<Equipment> equipment)
    {
        _characteristics = characteristics;

        CalculateFinalModel();
    }

    private void CalculateFinalModel()
    {
    }
}
