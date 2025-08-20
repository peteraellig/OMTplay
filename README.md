
Version 1.0.0.6

changelog OMTplay

1.0.0.4		works, but only with 2 channel setting or Master only (M or MA)  
1.0.0.5		works with 8 channels tested, you can choose which pair ( M or A or B or C)  
1.0.0.6		shows amount of received audio channels  

When I first read about OMT during the release of vMix29 and saw the DLLs, I was determined to get something like this up and running. This is the result. It runs flawlessly on all my PCs.  
With the help of Copilot and a lot of patience, **we** managed it. Copilot then also commented on the finished code neatly.  
All Intel machines with Nvidia or Intel graphics. Unfortunately, I was unable to control the bandwidth.  
In the first version of vMix29, a connection required approx. 50-80 Mbits, in the current version it's approx. 150-170 Mbits. But maybe my first project was 50i and now it's 50p.   
Perhaps someone would like to continue the research on this application — or it might inspire ideas for a better app. For me, at the moment, it's “goal achieved.”  

Video soon worked perfectly with the BGRA format.  
Making Audio working was a bit more tricky.  

Audio Conversion: vMix/OMTMediaFrame to NAudio. The vMix/OMT protocol delivers audio frames in a planar 32-bit floating point format (FPA1).    
This means that for each channel, all samples are stored sequentially (channel 1 samples, then channel 2 samples, etc.), rather than interleaved.    
Steps for conversion to NAudio:  
1.	Receive Planar Audio Data:  
•	The OMTMediaFrame contains a pointer (Data) to the audio buffer.  
•	The buffer holds all samples for all channels in planar order.  
•	The number of samples per channel is given by SamplesPerChannel.  
•	The number of channels is given by Channels.  
2.	Copy Planar Data to Managed Array:  
•	The unmanaged buffer is copied into a managed float array using Marshal.Copy.  
•	The array size is SamplesPerChannel * Channels.  
3.	Convert Planar to Interleaved Format:  
•	NAudio expects interleaved audio: sample 1 of channel 1, sample 1 of channel 2, sample 2 of channel 1, sample 2 of channel 2, etc.  
•	The code loops over each sample index and each channel, rearranging the planar data into an interleaved float array.  
4.	Convert Float Array to Byte Array:  
•	The interleaved float array is converted to a byte array using Buffer.BlockCopy.  
•	Each float sample is 4 bytes.  
5.	Feed Data to NAudio Buffer:  
•	The byte array is added to the BufferedWaveProvider using AddSamples.  
•	The BufferedWaveProvider is configured for IEEE float format (matching the sample rate and channel count).  
Summary:  
•	vMix/OMT delivers planar float audio.  
•	The code converts it to interleaved float audio.  
•	The interleaved data is fed to NAudio for playback.  
