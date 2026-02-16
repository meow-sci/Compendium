using Brutal.ImGuiApi;
using KSA;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi;
using Brutal.StbApi.Texture;
using RenderCore;

namespace Compendium
{
public partial class Compendium
    {
       private static ImTextureRef texID;

        public static void RegisterPhotos ()
        {
                    var texture = new TextureAsset("Content/Compendium/Photos/compendium-logo-1.png", new(new StbTexture.LoadSettings { ForceChannels = 4 }));
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