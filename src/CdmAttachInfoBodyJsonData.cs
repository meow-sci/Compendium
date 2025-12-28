using Brutal.ImGuiApi;
using KSA;

namespace Compendium
{


    public partial class Compendium
    {

        public static void AttachInfoBodyJsonDict()
        {
            Console.WriteLine("Compendium: Attaching bodyJson data to celestial bodies...");
            // If somehow bodyJsonDict is null or empty, just return
            if (bodyJsonDict == null || bodyJsonDict.Count == 0)
            {
                Console.WriteLine("Compendium: HEY - bodyJsonDict IS NULL OR EMPTY - WHAT");
                return;
            }

            foreach (var kvp in bodyJsonDict)
            {

                // The key is the body ID, but it may need to be looked up with the system prefix or Compendium prefix, that isn't part of the bodyId itself.
                // For example, "Sol.Earth" or "Compendium.Earth" both refer to the body with ID "Earth".  We want bodyId to just be "Earth" here.
                string fullKey = kvp.Key;
                string bodyId = fullKey.Contains('.') ? fullKey.Substring(fullKey.LastIndexOf('.') + 1) : fullKey;

                var bodyJsonData = kvp.Value;
                // gets the celestial body by its ID
                var worldSun = Universe.WorldSun;
                Console.WriteLine($"Compendium: Looking for celestial body with ID: {bodyId} with WorldSun {worldSun}");
                Celestial? bodyCelestial = FindCelestialById(worldSun, bodyId);

                // First checks if the entry as key exists as a celestial body in the current universe - if it doesn't then skip it.
                if (bodyCelestial == null)
                {
                    Console.WriteLine($"Compendium: bodyCelestial is null for bodyId: '{bodyId}' / '{bodyCelestial}', skipping...");
                    continue;
                }

                Console.WriteLine($"Compendium: Attaching bodyJson data to celestial body: {bodyId}");
                bodyJsonData.OrbitLineMode = bodyCelestial.OrbitLineMode;
                bodyJsonData.DrawnUiBox = bodyCelestial.DrawnUiBox;
                // Mean radius in kilometers
                bodyJsonData.RadiusKm = (float)(bodyCelestial.MeanRadius / 1000f);
                // Mass values
                bodyJsonData.Mass = bodyCelestial.Mass;


                string massWithSIPrefix = FormatMassWithUnit(bodyCelestial.Mass);
                double earthMasses = bodyCelestial.Mass / 5.972168e24;
                double lunarMasses = bodyCelestial.Mass / 7.342e22;

                if (earthMasses > 0.01) { bodyJsonData.MassText = new ImString($"Mass: {bodyCelestial.Mass:E2} Kg / ({massWithSIPrefix}) / ({earthMasses:F3} Earths)"); }
                else if (lunarMasses > 0.00001) { bodyJsonData.MassText = new ImString($"Mass: {bodyCelestial.Mass:E2} Kg / ({massWithSIPrefix}) / ({lunarMasses:F5} Lunas)"); }
                else { bodyJsonData.MassText = new ImString($"Mass: {bodyCelestial.Mass:E2} Kg / ({massWithSIPrefix})"); }


                // Gravity values
                // Calculates gravity using the formula: g = G * M / R^2 and then what the surface gravity would be.
                // where G is the gravitational constant (6.67430 × 10^-11 m^3 kg^-1 s^-2), M is the mass in kg, R is the radius in meters.
                double gravity = 6.67430e-11 * bodyCelestial.Mass / (bodyCelestial.ObjectRadius * bodyCelestial.ObjectRadius);
                // if the gravity just found is less than 0.001 m/s², display it as up to six decimal places.
                if (gravity < 0.001) { bodyJsonData.GravityText = new ImString($"Gravity: {gravity:F6} m/s²");}
                else { bodyJsonData.GravityText = new ImString($"Gravity: {gravity:F3} m/s²"); }




                // calculates the escape velocity using the formula: v = sqrt(2 * G * M / R)
                var escapeVelocity = Math.Sqrt(2 * 6.67430e-11 * bodyCelestial.Mass / bodyCelestial.MeanRadius);
                bodyJsonData.EscapeVelocityText = new ImString($"Escape Velocity: {escapeVelocity / 1000f:F3} km/s");

                // Orbital period
                // Figures out the timescale we want to use for displaying the orbital period.  If it's more than 2 years, use years.  If it's more than 2 days, use days.  Otherwise use hours.
                double orbitalPeriodSeconds = bodyCelestial.Orbit.Period;
                
                if (orbitalPeriodSeconds >= 63072000) // More than 2 years
                {
                    double orbitalVal = orbitalPeriodSeconds / 31536000;
                    bodyJsonData.OrbitalDisplay = orbitalVal.ToString("F2") + " years";
                }
                else if (orbitalPeriodSeconds >= 172800) // More than 2 days
                {
                    double orbitalVal = orbitalPeriodSeconds / 86400;
                    bodyJsonData.OrbitalDisplay = orbitalVal.ToString("F2") + " days";
                }
                else // Use hours
                {
                    double orbitalVal = orbitalPeriodSeconds / 3600;
                    bodyJsonData.OrbitalDisplay = orbitalVal.ToString("F2") + " hours";
                }


                // Axial tilt
                // Gets the axial tilt values depending on whether the selected celestial's parent is the sun or another body.
  
                //ImString thisTiltText;
                if (bodyCelestial.Parent == Universe.WorldSun)
                { 
                    //thisTilt = selectedCelestial.GetCce2Cci().ToXyzRadians().X * (180.0 / Math.PI);
                    double thisTilt = bodyCelestial.BodyTemplate.Rotation.Tilt.ToDegrees();
                    ImString thisTiltText = new ImString($"{thisTilt:F2}°");
                    bodyJsonData.ThisTiltText = new ImString($"Axial Tilt: {thisTiltText}");
                }
                else
                {
                    double thisTilt = bodyCelestial.GetCci2Orb().Inverse().ToXyzRadians().X * (180.0 / Math.PI);
                    //string parentName = bodyCelestial.Parent.Id;
                    ImString thisTiltText = new ImString($"{thisTilt:F2}° ( Relative to {bodyCelestial.Parent.Id} )");
                    bodyJsonData.ThisTiltText = new ImString($"Axial Tilt: {thisTiltText}");
                } 
                // Eccentricity
                bodyJsonData.EccentricityText = new ImString($"Eccentricity: {bodyCelestial.Eccentricity:F4}");


                // Inclination 

                // First gets the inclination in degrees from radians.
                double inclinationDeg = bodyCelestial.Inclination * (180.0 / Math.PI);
                // If a body is not a child of the sun, it is a satellite of another body, so we need to get the inclination relative to its parent body's orbital plane.
                // We need to calculate the relative inclination by subtracting the parent's axial tilt from the body's inclination.
                if (bodyCelestial.Parent != Universe.WorldSun)
                {
                    double parentTiltDeg = bodyCelestial.GetCci2Orb().Inverse().ToXyzRadians().X * (180.0 / Math.PI);
                    double relativeInclination = inclinationDeg - parentTiltDeg;
                    bodyJsonData.InclinationText = new ImString($"Inclination: {relativeInclination:F2}° ( Relative to {bodyCelestial.Parent.Id} equator )");
                }
                else
                { bodyJsonData.InclinationText = new ImString($"Inclination: {inclinationDeg:F2}°"); }


                // Sidereal period and tidal locking
                // Gets the sidereal period in hours or days depending on length, and checks for tidal locking
                // For now uses .ToNearest() to return back the closest range to report the stat as..
                string siderealPeriod;
                // ImString siderealPeriodText;
                if (bodyCelestial.BodyTemplate.Rotation.IsTidallyLocked.Value == true || bodyCelestial.BodyTemplate.Rotation.SiderealPeriod == 0)
                {
                    bodyJsonData.TidalLockText = new ImString("Tidally locked rotation");
                }
                else
                {
                    siderealPeriod = bodyCelestial.BodyTemplate.Rotation.SiderealPeriod.ToNearest();
                    if (siderealPeriod != null)
                    {
                        bodyJsonData.SiderealPeriodText = new ImString($"Sidereal Period: {siderealPeriod:F2}");
                        bodyJsonData.TidalLockText = new ImString("False");
                    }
                }



                // Gets the Semi-Major and Semi-Minor axes in AU for display if they are large enough - we only need a float.
                // Keep in mind that both values are saved in game as meters, so we need divide by the appropritate factor to get m to AU.  1 AU = 1.496e+11 m
                float semiMajorAxisAU = (float)bodyCelestial.SemiMajorAxis / 1.496e+11f; // Convert m to AU
                float semiMinorAxisAU = (float)bodyCelestial.SemiMinorAxis / 1.496e+11f; // Convert m to AU
                // Next saves the value of each in km as strings adding thousands separators for easier reading.
                // Clamps the decimal places to 1 for cleaner display.
                string semiMajorAxisKm = (bodyCelestial.SemiMajorAxis / 1000f).ToString("N1");
                string semiMinorAxisKm = (bodyCelestial.SemiMinorAxis / 1000f).ToString("N1");
                // If the semi-major axis is less than 0.1 AU, display it in AU - otherwise display with both km and then AU. Use the same semimajor test for both it and semiminor printing
                if (semiMajorAxisAU < 0.1f)
                {
                    bodyJsonData.SemiMajorAxisText = new ImString($"Semi-Major Axis: {semiMajorAxisKm} km");
                    bodyJsonData.SemiMinorAxisText = new ImString($"Semi-Minor Axis: {semiMinorAxisKm} km");
                }
                else
                {
                    bodyJsonData.SemiMajorAxisText = new ImString($"Semi-Major Axis: {semiMajorAxisKm} km / {semiMajorAxisAU:F3} AU");
                    bodyJsonData.SemiMinorAxisText = new ImString($"Semi-Minor Axis: {semiMinorAxisKm} km / {semiMinorAxisAU:F3} AU");
                }


                if (bodyCelestial.Orbit.GetType().Name != "Elliptical")
                { bodyJsonData.OrbitTypeText = new ImString($"Orbit Type: {bodyCelestial.Orbit.GetType().Name}"); }
                else
                { bodyJsonData.OrbitTypeText = new ImString("Elliptical"); }

                }
            }
        
    }
}