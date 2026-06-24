namespace TrafficSim
{
    /// <summary>
    /// Contract for all vehicle behaviors.
    /// </summary>
    public interface IVehicleState
    {
        void Enter(VehicleController vehicle);
        void Update(VehicleController vehicle);
    }
}
