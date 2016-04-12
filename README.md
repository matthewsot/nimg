# NImg
Extremely lossy image compression with neural networks

Uses the limited, slow, and inefficient (but super simple and easy-to-use!) feed-forward neural network [Zoltar](https://github.com/matthewsot/zoltar).

# Show me the numbers
Original PNG image, **106,638 bytes** uncompressed and 106,795 bytes when compressed with the default Windows "send-to" compression:

![Original PNG image](Images/original.png "Original PNG image")

**106,638 bytes**

Original image saved as a JPEG in Paint.net, 53,185 bytes as ``.jpg``, **53,067 bytes** after compressed to ``.zip``:

![Original JPEG image](Images/original.jpg "Original JPEG image")

**53,067 bytes**

Using ``nimg.config`` file:
```
inputPixels 3
innerLayers 2
neuronsPerLayer 3
colorIndexBytes 2
writeTolerance 10
colorIndexTolerance 5
trainingRounds 25
```

Compressed into an NIMG file of **50,040 bytes**. The ``.nimg`` file (after being converted losslessly to a PNG):

![Reconstructed NIMG image](Images/reconstructed.png "Reconstructed NIMG image")

**50,040 bytes**

Note that because NIMG is so lossy (see the "Drawbacks" section), the recreated file is **not** the same as the original PNG. NIMG has an option to recreate the NIMG file into a PNG, which is exactly the same image as the compressed NIMG file.

The reconstructed ``.nimg`` file (exactly the same image as the ``.nimg``), when saved as a PNG, has a file size of **61,040 bytes**.
Saving that PNG file as a JPEG in Paint.net creates a ``.jpg`` with file size 57,691 bytes, **57,565 bytes** after zip-compressing in Windows.

All in all:
- **53.1% reduction** from the original PNG
- **5.7% reduction** from the original JPG
- **18% reduction** from the reconstructed image saved as a PNG
- **13.1% reduction** from the reconstructed image saved as a compressed JPEG.

# Why is it so good?

There are a few things contributing to the size reduction:

- There's no metadata, so NIMG gets to cheat a little bit. The ``.nimg`` format is currently very sparse, so 100% of the file size is going towards encoding the image.
- The actual ``.nimg`` file is very lossy when compared to the original (see the "Drawbacks" section), though it's worth noting that even compared to the exact same JPG and PNG files (aka no loss), the NIMG file is consistently smaller.
- It comes bundled with a neural network that took ~2 minutes to train on an i7-4790k. That's partly because the neural network code I used is inefficient, but it's also a built-in issue with the format.

# Why is it so bad?

5-20% size reduction compared to a standard JPEG/PNG file is, admittedly, not that impressive. A few reasons it's not better:

- The neural network used is a super simple feed-forward network. All it does is look at the previous n pixels to the left of the current pixel and attempt to predict the next one. An improved NIMG would implement a more complex neural network and train it longer/more efficiently.
- Neural networks aren't magic, and to get one that fits the image extremely well would probably take more space to store the weights than it would actually save in the file (not unlike a standard compression format's dictionary size).

# Drawbacks & Limitations

First and foremost, NIMG requires that you train a neural network on the image file before compression. This can take anywhere between 20 seconds and a day, depending on the level of compression desired and the resolution of the file.

On top of that, the NIMG file format takes quite a few shortcuts to arrive at a small file size. A few of those:
- By default NIMG uses a color dictionary, which treats all colors within a certain tolerance interval as a single color. Generally this won't get in the way (the demo Bliss image above has a tolerance of just 5 R/G/B units), but for complex images you might need to disable the color dictionary by setting ``colorIndexBytes`` to 0
- The color dictionary's bytes are limited to either 239 or 61440 colors, and a higher limit leads to an almost 2x file size. Again, if you need complex colors you should disable the color dictionary, though that significantly impacts file size.
- NIMG is inherently lossy. Setting both tolerances in ``nimg.config`` to 1 will make the file practically lossless, but it is highly unlikely that the network will output exactly the right 3 values for any of the pixels, basically making NIMG useless.
- And more, check out ``Compressor.cs`` for a better idea of the shortcuts taken

# What's the use case for this?

Because of the mentioned drawbacks, NIMG is *probably not* a great solution for compressing individual files. With that said, a few situations where applying NIMG might make a meaningful difference:

- Video compression. You could theoretically have a small set of network models and color dictionaries for thousands of frames, which would multiply the benefits of NIMG.
- Websites with similar images, like a Flickr search results page for "sky." Similar images should work fairly well with a single network model and color dictionary, so you could compress large numbers of images with one shared network.

Essentially wherever multiple similar images are sent NIMG could be useful.

# Getting Started with NIMG

Before using NIMG it's recommended to create a ``nimg.config`` file in the same directory that ``nimg.exe`` is run from. It should look like:

```
inputPixels [How many pixels back to use when predicting the next pixel]
innerLayers [How many inner layers in the neural network]
neuronsPerLayer [How many neurons per inner layer]
colorIndexBytes [ 0 => Disable the color dictionary, 1 => maximum 239 distinct colors, 2 => maximum 61440 colors. Higher = larger file size ]
writeTolerance [ The maximum difference between the predicted color and the actual color. Lower = large file size, less lossy ]
colorIndexTolerance [ The maximum difference between the pixel color and the color stored in the dictionary. Lower = larger file size, less lossy ]
trainingRounds [ The number of rounds to train the network for. Lower = higher file size, quicker ]
```

See above for an example ``nimg.config``. Any lines may be removed and will be filled in with default values.

To compress ``image.png`` into ``image.nimg``:
```
nimg.exe compress image.png
```

To convert ``image.nimg`` back into a PNG (which will be named ``image.png``):
```
nimg.exe reconstruct image.nimg
```