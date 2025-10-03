# KuzuDot\libkuzu

To use the latest KuzuDB native library, you need to download the `kuzu_shared.dll` file and place it in this folder.

Download the latest `libkuzu-windows-x86_64.zip` release from [Github](https://github.com/kuzudb/kuzu/releases) and extract the contents here.

The KuzuDot projects are set up to pull `kuzu_shared.dll` from here and place it in their output folders.

If you have a working version of `KuzuDot.dll`, it should still be able to call to new versions of the `kuzu_shared.dll` without needing to be changed. So even just replacing `kuzu_shared.dll` in your project will allow you to use new KuzuDB features without rebuilding KuzuDot.If you have a working version of `KuzuDot.dll`, it should still be able to call to new versions of the `kuzu_shared.dll` without needing to be changed. So even just replacing `kuzu_shared.dll` in your project will allow you to use new KuzuDB features without rebuilding KuzuDot.