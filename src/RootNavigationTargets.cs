using Brutal.Numerics;
using KSA;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Compendium
{
    internal static class RootNavigationTargets
    {
        private sealed class TargetState(Astronomical body)
        {
            public Astronomical? Body = body;
        }

        private static readonly ConditionalWeakTable<Vehicle, TargetState> vehicleTargets = new();
        // The simulation's FlightComputer copies share their original BurnPlan.
        private static readonly ConditionalWeakTable<BurnPlan, TargetState> computerTargets = new();
        private readonly record struct TargetRequest(Vehicle Vehicle, Astronomical? Body, FlightComputerAttitudeTrackTarget? Tracking);
        private static readonly ConcurrentQueue<TargetRequest> requests = new();

        public static bool PatchesInstalled { get; set; }

        public static void QueueSelection(Vehicle vehicle, Astronomical? body)
        {
            requests.Enqueue(new TargetRequest(vehicle, body, null));
        }

        public static void QueueTracking(Vehicle vehicle, Astronomical body, FlightComputerAttitudeTrackTarget tracking)
        {
            if (tracking is not (FlightComputerAttitudeTrackTarget.Toward or FlightComputerAttitudeTrackTarget.Away))
            {
                throw new ArgumentOutOfRangeException(nameof(tracking));
            }
            requests.Enqueue(new TargetRequest(vehicle, body, tracking));
        }

        // Apply vehicle changes alongside the game's own input events, not during GUI rendering.
        public static void ApplyRequests()
        {
            while (requests.TryDequeue(out var request))
            {
                if (request.Vehicle.IsDisposed || (request.Body != null && !IsLoadedRoot(request.Body)))
                {
                    Console.WriteLine("Compendium: root target request discarded because its vehicle or body is no longer loaded.");
                    continue;
                }
                if (request.Tracking.HasValue)
                {
                    if (GetTarget(request.Vehicle) != request.Body || !request.Vehicle.IsControllable)
                    {
                        Console.WriteLine("Compendium: root tracking request discarded because the target changed or the vehicle is not controllable.");
                        continue;
                    }
                    InputEvents.FlightComputerInputBuffer.Add(new InputEvents.FlightComputerInputData
                    {
                        Vehicle = request.Vehicle,
                        EnumValue = request.Tracking.Value
                    });
                }
                else if (request.Body != null)
                {
                    Select(request.Vehicle, request.Body);
                }
                else
                {
                    Clear(request.Vehicle);
                }
            }
        }

        public static bool Supports(Astronomical body)
        {
            return body is FixedStar or Barycenter && body is IIndependentRoot && body is not IOrbiter;
        }

        private static bool IsLoadedRoot(Astronomical body)
        {
            if (body.System == Universe.CurrentSystem)
            {
                foreach (var root in Universe.Roots)
                {
                    if (ReferenceEquals(root, body))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Astronomical? GetTarget(Vehicle vehicle)
        {
            return vehicle.Target == null && vehicleTargets.TryGetValue(vehicle, out var state)
                ? GetLoadedBody(state) : null;
        }

        private static Astronomical? GetLoadedBody(TargetState state)
        {
            var body = Volatile.Read(ref state.Body);
            if (body == null || !IsLoadedRoot(body))
            {
                return null;
            }
            return body;
        }

        public static void Select(Vehicle vehicle, Astronomical body)
        {
            if (!PatchesInstalled || !Supports(body) || !IsLoadedRoot(body))
            {
                throw new InvalidOperationException($"Compendium cannot select root navigation target '{body.Id}'.");
            }

            vehicle.SetTarget(null);
            var state = new TargetState(body);
            vehicleTargets.Remove(vehicle);
            vehicleTargets.Add(vehicle, state);
            computerTargets.Remove(vehicle.FlightComputer.BurnPlan);
            computerTargets.Add(vehicle.FlightComputer.BurnPlan, state);
            Console.WriteLine($"Compendium: navigation target '{body.Id}' selected for '{vehicle.Id}' (session only).");
        }

        public static void Clear(Vehicle vehicle, bool stopTracking = true)
        {
            if (!vehicleTargets.TryGetValue(vehicle, out var state))
            {
                return;
            }
            Volatile.Write(ref state.Body, null);
            vehicleTargets.Remove(vehicle);
            computerTargets.Remove(vehicle.FlightComputer.BurnPlan);
            if (stopTracking)
            {
                StopRootTracking(vehicle.FlightComputer);
                if (vehicle.NavBallData.Frame == VehicleReferenceFrame.Dock)
                {
                    vehicle.SetNavBallFrame(VehicleReferenceFrame.EclBody);
                }
            }
        }

        private static void StopRootTracking(FlightComputer computer)
        {
            if (computer.AttitudeFrame == VehicleReferenceFrame.Dock ||
                computer.AttitudeTrackTarget is FlightComputerAttitudeTrackTarget.Toward or FlightComputerAttitudeTrackTarget.Away)
            {
                computer.SetInertialNullRot();
            }
        }

        public static void Unload()
        {
            requests.Clear();
            RootClosestApproach.Clear();
            foreach (var entry in vehicleTargets)
            {
                Clear(entry.Key);
            }
            vehicleTargets.Clear();
            computerTargets.Clear();
            PatchesInstalled = false;
        }

        public static NavigationTarget CreateNavigationTarget(Astronomical body, IParentBody parent, UniverseTime time)
        {
            double3 parentPosition = parent is IOrbiter positionOrbiter
                ? positionOrbiter.GetPositionEcl(time) : parent.GetPositionEcl();
            double3 parentVelocity = parent is IOrbiter velocityOrbiter
                ? velocityOrbiter.GetVelocityEcl(time) : parent.GetVelocityEcl();
            doubleQuat ecl2Cci = parent.GetCce2Cci();
            return new NavigationTarget
            {
                PositionCci = (body.GetPositionEcl() - parentPosition).Transform(ecl2Cci),
                VelocityCci = (body.GetVelocityEcl() - parentVelocity).Transform(ecl2Cci),
                Dock2Cci = VehicleReferenceFrameEx.GetDock2Cci(doubleQuat.Concatenate(body.GetBodyFixed2Ecl(), ecl2Cci), doubleQuat.Identity),
                DockFrameRatesCci = body.GetBodyRates().Transform(doubleQuat.Concatenate(body.GetBodyFixed2Ecl(), ecl2Cci))
            };
        }

        public static NavigationTarget? CreateMarkerTarget(IOrbiter? target, Part? targetPart, IParentBody parent, UniverseTime time, Vehicle vehicle)
        {
            var root = GetTarget(vehicle);
            return root != null
                ? CreateNavigationTarget(root, parent, time)
                : NavigationTarget.Create(target, targetPart, parent, time);
        }

        public static void ApplyControlTarget(FlightComputer computer, ref FlightComputerNavigation nav)
        {
            if (nav.Target == null &&
                (computer.AttitudeTrackTarget is FlightComputerAttitudeTrackTarget.Toward or FlightComputerAttitudeTrackTarget.Away ||
                 (computer.AttitudeFrame == VehicleReferenceFrame.Dock &&
                  computer.AttitudeTrackTarget is FlightComputerAttitudeTrackTarget.None or FlightComputerAttitudeTrackTarget.Custom)) &&
                computerTargets.TryGetValue(computer.BurnPlan, out var state))
            {
                var body = GetLoadedBody(state);
                if (body != null)
                {
                    nav.Target = CreateNavigationTarget(body, nav.Parent, nav.Time);
                }
                else
                {
                    StopRootTracking(computer);
                }
            }
        }
    }
}
