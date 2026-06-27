using Brutal.ImGuiApi;
using KSA;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi;
//using Brutal.TextureApi;
//using Brutal.TextureApi.Stb;
//using System.IO;

using RenderCore;

namespace Compendium
{
public partial class Compendium
    {
       private static ImTextureRef texID;

        public static void RegisterPhotos ()
        {
                    var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                    var photoPath = Path.Combine(assemblyPath, "..", "Compendium", "Photos", "compendium-logo-1.png");
                    var texture = new TextureAsset(photoPath, new Brutal.TextureApi.Stb.Loader.LoadSettings { ForceChannels = 4 });
                    var renderer = KSA.Program.GetRenderer();
                    SimpleVkTexture vkTex;
                    
                    using (var stagingPool = renderer.Device.CreateStagingPool(renderer.Graphics, 1))
                    { 
                        vkTex = new SimpleVkTexture(renderer.Device, stagingPool, texture); 
                        }

                    var sampler = renderer.Device.CreateSampler(Presets.Sampler.SamplerPointClamped, null);

                    texID = ImGuiBackend.Vulkan.AddTexture(sampler, vkTex.ImageView);
        
        }






    }


}