using Brutal.Numerics;
using KSA;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Compendium
{
    internal readonly record struct RootApproach(double Distance, UniverseTime Time);

    internal static class RootClosestApproach
    {
        internal const double HorizonSeconds = 10 * 365 * 86400.0;
        private const int Samples = 512;
        private sealed class Cache
        {
            public Astronomical? Root;
            public FlightPlan? Plan;
            public long Updated;
            public UniverseTime Now;
            public RootApproach? Approach;
        }

        private static readonly ConditionalWeakTable<Vehicle, Cache> cache = new();

        public static void Clear() => cache.Clear();

        public static RootApproach? Get(Vehicle vehicle, Astronomical root, UniverseTime now)
        {
            var entry = cache.GetOrCreateValue(vehicle);
            if (entry.Root == root && entry.Plan == vehicle.FlightPlan && now >= entry.Now &&
                (now - entry.Now).Seconds() < 60 &&
                (!entry.Approach.HasValue || entry.Approach.Value.Time >= now) &&
                Stopwatch.GetElapsedTime(entry.Updated).TotalSeconds < 1)
            {
                return entry.Approach;
            }

            entry.Root = root;
            entry.Plan = vehicle.FlightPlan;
            entry.Now = now;
            entry.Updated = Stopwatch.GetTimestamp();
            entry.Approach = null;
            entry.Approach = Calculate(vehicle.FlightPlan, root, now);
            return entry.Approach;
        }

        internal static RootApproach? Calculate(FlightPlan plan, Astronomical root, UniverseTime now)
        {
            RootApproach? best = null;
            UniverseTime limit = now + HorizonSeconds;
            foreach (var patch in plan.Patches)
            {
                UniverseTime start = UniverseTime.Max(now, patch.StartTime);
                UniverseTime end = UniverseTime.Min(limit, patch.EndTime);
                if (end < start)
                {
                    continue;
                }

                double duration = (end - start).Seconds();
                // For a direct orbit of this root, one revolution contains every distance.
                if (ReferenceEquals(patch.Orbit.Parent, root) && patch.Orbit.IsBound() &&
                    double.IsFinite(patch.Orbit.Period) && patch.Orbit.Period > 0)
                {
                    duration = Math.Min(duration, patch.Orbit.Period);
                }
                double DistanceSquared(double offset)
                {
                    UniverseTime time = start + offset;
                    var orbit = patch.Orbit;
                    var parent = orbit.Parent;
                    double3 parentPosition = parent is IOrbiter orbiter
                        ? orbiter.GetPositionEcl(time) : parent.GetPositionEcl();
                    double3 position = parentPosition +
                        orbit.GetStateVectorsAt(time).PositionCci.Transform(parent.GetCci2Cce());
                    return (position - root.GetPositionEcl()).LengthSquared();
                }

                var minimum = FindMinimum(DistanceSquared, duration);
                if (minimum.HasValue)
                {
                    Consider(new RootApproach(Math.Sqrt(minimum.Value.SquaredDistance), start + minimum.Value.Offset));
                }
                if (ReferenceEquals(patch.Orbit.Parent, root))
                {
                    var periapsis = patch.Orbit.GetNextPeriapsisTime(start);
                    if (periapsis.HasValue && periapsis.Value >= start && periapsis.Value <= end)
                    {
                        double squaredDistance = DistanceSquared((periapsis.Value - start).Seconds());
                        if (double.IsFinite(squaredDistance) && squaredDistance >= 0)
                        {
                            Consider(new RootApproach(Math.Sqrt(squaredDistance), periapsis.Value));
                        }
                    }
                }
            }
            return best;

            void Consider(RootApproach candidate)
            {
                if (!best.HasValue || candidate.Distance < best.Value.Distance ||
                    (candidate.Distance == best.Value.Distance && candidate.Time < best.Value.Time))
                {
                    best = candidate;
                }
            }
        }

        internal static (double Offset, double SquaredDistance)? FindMinimum(Func<double, double> distanceSquared, double duration)
        {
            if (!double.IsFinite(duration) || duration < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }
            double bestTime = 0;
            double best = double.PositiveInfinity;
            void Evaluate(double time, double value)
            {
                if (!double.IsFinite(value) || value < 0)
                {
                    throw new InvalidOperationException("Compendium root approach prediction returned an invalid distance.");
                }
                if (value < best || (value == best && time < bestTime))
                {
                    best = value;
                    bestTime = time;
                }
            }

            double previousTime = 0;
            double previousValue = distanceSquared(0);
            Evaluate(0, previousValue);
            if (duration == 0)
            {
                return (bestTime, best);
            }

            double beforeTime = 0;
            double beforeValue = previousValue;
            void Refine(double left, double right)
            {
                const double ratio = 0.6180339887498949;
                double a = right - ratio * (right - left);
                double b = left + ratio * (right - left);
                double fa = distanceSquared(a);
                double fb = distanceSquared(b);
                for (int iteration = 0; iteration < 48; iteration++)
                {
                    Evaluate(a, fa);
                    Evaluate(b, fb);
                    if (fa <= fb)
                    {
                        right = b;
                        b = a;
                        fb = fa;
                        a = right - ratio * (right - left);
                        fa = distanceSquared(a);
                    }
                    else
                    {
                        left = a;
                        a = b;
                        fa = fb;
                        b = left + ratio * (right - left);
                        fb = distanceSquared(b);
                    }
                }
                Evaluate(a, fa);
                Evaluate(b, fb);
            }
            // Combine uniform coverage with logarithmic near-term coverage.
            var times = new SortedSet<double> { 0, duration };
            double logRange = Math.Log(1 + duration);
            for (int i = 1; i < Samples; i++)
            {
                times.Add(duration * i / Samples);
                times.Add(Math.Exp(logRange * i / Samples) - 1);
            }
            foreach (double time in times)
            {
                if (time == 0)
                {
                    continue;
                }
                double value = distanceSquared(time);
                Evaluate(time, value);
                if (previousTime == 0)
                {
                    Refine(0, time);
                }
                if (previousTime > 0 && previousValue <= beforeValue && previousValue <= value &&
                    (previousValue < beforeValue || previousValue < value))
                {
                    Refine(beforeTime, time);
                }
                if (time == duration)
                {
                    Refine(previousTime, time);
                }
                beforeTime = previousTime;
                beforeValue = previousValue;
                previousTime = time;
                previousValue = value;
            }
            return (bestTime, best);
        }
    }
}
