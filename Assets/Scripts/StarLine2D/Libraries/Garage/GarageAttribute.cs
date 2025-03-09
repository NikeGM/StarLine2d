using UnityEngine;

namespace StarLine2D.Libraries.Garage
{
    public class GarageAttribute : PropertyAttribute
    {
        public GarageItem.ShipType Filter { get; private set; }
        public bool HasFilter { get; private set; }

        public GarageAttribute(GarageItem.ShipType filter)
        {
            Filter = filter;
            HasFilter = true;
        }

        public GarageAttribute()
        {
            HasFilter = false;
        }
    }
}