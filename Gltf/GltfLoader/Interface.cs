using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GltfLoader.Schema;

namespace GltfLoader
{
    public static class Interface
    {
        const uint GLTFHEADER = 0x46546C67;
        const uint GLTFVERSION2 = 2;
        const uint CHUNKJSON = 0x4E4F534A;
        const uint CHUNKBIN = 0x004E4942;

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";

        const string EMBEDDEDPNG = "data:image/png;base64,";
        const string EMBEDDEDJPEG = "data:image/jpeg;base64,";

        /// <summary>
        /// Loads a <code>Schema.Gltf</code> model from a file
        /// </summary>
        /// <param name="filePath">Source file path to a gltf/glb model</param>
        /// <returns><code>Schema.Gltf</code> model</returns>
        public static ValueTask<Gltf> LoadModelAsync(string filePath)
        {
            var path = Path.GetFullPath(filePath);

            using Stream stream = File.OpenRead(path);
            return LoadModelAsync(stream);
        }

        /// <summary>
        /// Reads a <code>Schema.Gltf</code> model from a stream
        /// </summary>
        /// <param name="stream">Readable stream to a gltf/glb model</param>
        /// <returns><code>Schema.Gltf</code> model</returns>
        public static ValueTask<Gltf> LoadModelAsync(Stream stream)
        {
            bool binaryFile = false;

            uint magic = 0;
            magic |= (uint)stream.ReadByte();
            magic |= (uint)stream.ReadByte() << 8;
            magic |= (uint)stream.ReadByte() << 16;
            magic |= (uint)stream.ReadByte() << 24;

            if (magic == GLTFHEADER) binaryFile = true;

            stream.Position = 0; // restart read position

            Stream fileData;

            if (binaryFile)
            {
                fileData = new MemoryStream(ParseBinary(stream));
            }
            else
            {
                fileData = stream;
            }

            return JsonSerializer.DeserializeAsync<Gltf>(fileData);
        }

        private static byte[] ParseBinary(Stream stream)
        {
            using BinaryReader binaryReader = new BinaryReader(stream);
            ReadBinaryHeader(binaryReader);

            return ReadBinaryChunk(binaryReader, CHUNKJSON);
        }

        private static byte[] ReadBinaryChunk(BinaryReader binaryReader, uint format)
        {
            while (true) // keep reading until EndOfFile exception
            {
                uint chunkLength = binaryReader.ReadUInt32();

                if ((chunkLength & 3) != 0)
                {
                    throw new InvalidDataException($"The chunk must be padded to 4 bytes: {chunkLength}");
                }

                uint chunkFormat = binaryReader.ReadUInt32();

                var data = binaryReader.ReadBytes((int)chunkLength);

                if (chunkFormat == format) return data;
            }
        }

        /// <summary>
        /// Loads the binary buffer chunk of a glb file
        /// </summary>
        /// <param name="filePath">Source file path to a glb model</param>
        /// <returns>Byte array of the buffer</returns>
        public static byte[] LoadBinaryBuffer(string filePath)
        {
            using Stream stream = File.OpenRead(filePath);
            return LoadBinaryBuffer(stream);
        }

        /// <summary>
        /// Reads the binary buffer chunk of a glb stream
        /// </summary>
        /// <param name="stream">Readable stream to a glb model</param>
        /// <returns>Byte array of the buffer</returns>
        public static byte[] LoadBinaryBuffer(Stream stream)
        {
            using BinaryReader binaryReader = new BinaryReader(stream);
            ReadBinaryHeader(binaryReader);

            return ReadBinaryChunk(binaryReader, CHUNKBIN);
        }

        private static void ReadBinaryHeader(BinaryReader binaryReader)
        {
            uint magic = binaryReader.ReadUInt32();
            if (magic != GLTFHEADER)
            {
                throw new InvalidDataException($"Unexpected magic number: {magic}");
            }

            uint version = binaryReader.ReadUInt32();
            if (version != GLTFVERSION2)
            {
                throw new InvalidDataException($"Unknown version number: {version}");
            }

            uint length = binaryReader.ReadUInt32();
            long fileLength = binaryReader.BaseStream.Length;
            if (length != fileLength)
            {
                throw new InvalidDataException($"The specified length of the file ({length}) is not equal to the actual length of the file ({fileLength}).");
            }
        }

        /// <summary>
        /// Creates an External File Solver for a given gltf file path, so we can resolve references to associated files
        /// </summary>
        /// <param name="gltfFilePath">ource file path to a gltf/glb model</param>
        /// <returns>Lambda funcion to resolve dependencies</returns>
        private static Func<string, Byte[]> GetExternalFileSolver(string gltfFilePath)
        {
            return asset =>
            {
                if (string.IsNullOrEmpty(asset)) return LoadBinaryBuffer(gltfFilePath);
                var bufferFilePath = Path.Combine(Path.GetDirectoryName(gltfFilePath), asset);
                return File.ReadAllBytes(bufferFilePath);
            };
        }

        /// <summary>
        /// Gets a binary buffer referenced by a specific <code>Schema.Buffer</code>
        /// </summary>
        /// <param name="model">The <code>Schema.Gltf</code> model containing the <code>Schema.Buffer</code></param>
        /// <param name="bufferIndex">The index of the buffer</param>
        /// <param name="gltfFilePath">Source file path used to load the model</param>
        /// <returns>Byte array of the buffer</returns>
        public static Byte[] LoadBinaryBuffer(this Gltf model, int bufferIndex, string gltfFilePath)
        {
            return LoadBinaryBuffer(model, bufferIndex, GetExternalFileSolver(gltfFilePath));
        }

        /// <summary>
        /// Opens a stream to the image referenced by a specific <code>Schema.Image</code>
        /// </summary>
        /// <param name="model">The <code>Schema.Gltf</code> model containing the <code>Schema.Buffer</code></param>
        /// <param name="imageIndex">The index of the image</param>
        /// <param name="gltfFilePath">Source file path used to load the model</param>
        /// <returns>An open stream to the image</returns>
        public static Stream OpenImageFile(this Gltf model, int imageIndex, string gltfFilePath)
        {
            return OpenImageFile(model, imageIndex, GetExternalFileSolver(gltfFilePath));
        }

        /// <summary>
        /// Gets a binary buffer referenced by a specific <code>Schema.Buffer</code>
        /// </summary>
        /// <param name="model">The <code>Schema.Gltf</code> model containing the <code>Schema.Buffer</code></param>
        /// <param name="bufferIndex">The index of the buffer</param>
        /// <param name="externalReferenceSolver">An user provided lambda function to resolve external assets</param>
        /// <returns>Byte array of the buffer</returns>
        /// <remarks>
        /// Binary buffers can be stored in three different ways:
        /// - As stand alone files.
        /// - As a binary chunk within a glb file.
        /// - Encoded to Base64 within the JSON.
        /// 
        /// The external reference solver funcion is called when the buffer is stored in an external file,
        /// or when the buffer is in the glb binary chunk, in which case, the Argument of the function will be Null.
        /// 
        /// The Lambda function must return the byte array of the requested file or buffer.
        /// </remarks>
        public static Byte[] LoadBinaryBuffer(this Gltf model, int bufferIndex, Func<string, Byte[]> externalReferenceSolver)
        {
            var buffer = model.Buffers[bufferIndex];

            var bufferData = LoadBinaryBufferUnchecked(buffer, externalReferenceSolver);

            // As per https://github.com/KhronosGroup/glTF/issues/1026
            // Due to buffer padding, buffer length can be equal or larger than expected length by only 3 bytes
            if (bufferData.Length < buffer.ByteLength || (bufferData.Length - buffer.ByteLength) > 3)
            {
                throw new InvalidDataException($"The buffer length is {bufferData.Length}, expected {buffer.ByteLength}");
            }

            return bufferData;
        }

        private static byte[] TryLoadBase64BinaryBufferUnchecked(Schema.Buffer buffer, string prefix)
        {
            if (buffer.Uri == null || !buffer.Uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var content = buffer.Uri.Substring(prefix.Length);
            return Convert.FromBase64String(content);
        }

        private static byte[] LoadBinaryBufferUnchecked(Schema.Buffer buffer, Func<string, byte[]> externalReferenceSolver)
        {
            return TryLoadBase64BinaryBufferUnchecked(buffer, EMBEDDEDGLTFBUFFER)
                ?? TryLoadBase64BinaryBufferUnchecked(buffer, EMBEDDEDOCTETSTREAM)
                ?? externalReferenceSolver(buffer?.Uri);
        }

        /// <summary>
        /// Opens a stream to the image referenced by a specific <code>Schema.Image</code>
        /// </summary>
        /// <param name="model">The <code>Schema.Gltf</code> model containing the <code>Schema.Image</code></param>
        /// <param name="imageIndex">The index of the image</param>
        /// <param name="externalReferenceSolver">An user provided lambda function to resolve external assets</param>
        /// <returns>An open stream to the image</returns>
        /// <remarks>
        /// Images can be stored in three different ways:
        /// - As stand alone files.
        /// - As a part of binary buffer accessed via bufferView.
        /// - Encoded to Base64 within the JSON.
        /// 
        /// The external reference solver funcion is called when the image is stored in an external file,
        /// or when the image is in the glb binary chunk, in which case, the Argument of the function will be Null.
        /// 
        /// The Lambda function must return the byte array of the requested file or buffer.
        /// </remarks>
        public static Stream OpenImageFile(this Gltf model, int imageIndex, Func<string, byte[]> externalReferenceSolver)
        {
            var image = model.Images[imageIndex];

            if (image.BufferView.HasValue)
            {
                var bufferView = model.BufferViews[image.BufferView.Value];

                var bufferBytes = model.LoadBinaryBuffer(bufferView.Buffer, externalReferenceSolver);

                return new MemoryStream(bufferBytes, bufferView.ByteOffset, bufferView.ByteLength);
            }

            if (image.Uri.StartsWith("data:image/")) return OpenEmbeddedImage(image);

            var imageData = externalReferenceSolver(image.Uri);

            return new MemoryStream(imageData);
        }

        private static Stream OpenEmbeddedImage(Image image)
        {
            string content = null;

            if (image.Uri.StartsWith(EMBEDDEDPNG)) content = image.Uri.Substring(EMBEDDEDPNG.Length);
            if (image.Uri.StartsWith(EMBEDDEDJPEG)) content = image.Uri.Substring(EMBEDDEDJPEG.Length);

            var bytes = Convert.FromBase64String(content);
            return new MemoryStream(bytes);
        }

        /// <summary>
        /// Parses a JSON formatted text content
        /// </summary>
        /// <param name="fileData">JSON text content</param>
        /// <returns><code>Schema.Gltf</code> model</returns>
        public static ValueTask<Gltf> DeserializeModelAsync(string fileData)
        {
            return JsonSerializer.DeserializeAsync<Gltf>(new MemoryStream(Encoding.UTF8.GetBytes(fileData)));
        }

        /// <summary>
        /// Serializes a <code>Schema.Gltf</code> model to text
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <returns>JSON formatted text</returns>
        public static string SerializeModel(this Gltf model)
        {
            return Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(model, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Saves a <code>Schema.Gltf</code> model to a gltf file
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="path">Destination file path</param>
        public static void SaveModel(this Gltf model, string path)
        {
            using Stream stream = File.Create(path);
            SaveModel(model, stream);
        }

        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable stream
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="stream">Writable stream</param>
        public static void SaveModel(this Gltf model, Stream stream)
        {
            string fileData = SerializeModel(model);

            using var ts = new StreamWriter(stream);
            ts.Write(fileData);
        }

        /// <summary>
        /// Saves a <code>Schema.Gltf</code> model to a glb file
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="buffer">Binary buffer to embed in the file, or null</param>
        /// <param name="filePath">Destination file path</param>
        public static void SaveBinaryModel(this Gltf model, byte[] buffer, string filePath)
        {
            using Stream stream = File.Create(filePath);
            SaveBinaryModel(model, buffer, stream);
        }

        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable stream
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="buffer">Binary buffer to embed in the file, or null</param>
        /// <param name="stream">Writable stream</param>
        public static void SaveBinaryModel(this Gltf model, byte[] buffer, Stream stream)
        {
            using var wb = new BinaryWriter(stream);
            SaveBinaryModel(model, buffer, wb);
        }

        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable binary writer
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="buffer">Binary buffer to embed in the file, or null</param>
        /// <param name="binaryWriter">Binary Writer</param>
        public static void SaveBinaryModel(this Gltf model, byte[] buffer, BinaryWriter binaryWriter)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var brefcount = model.Buffers == null ? 0 : model.Buffers.Count(item => item.Uri == null);

            if (brefcount > 1)
                throw new ArgumentNullException($"{nameof(model)} multiple binary buffer references found");

            if (brefcount == 1)
            {
                if (buffer == null) throw new ArgumentNullException($"{nameof(buffer)} must not be null");

                var b = model.Buffers[0];

                if (b.ByteLength > buffer.Length) throw new ArgumentException($"{nameof(buffer)} byte size is smaller than declared");
                if ((buffer.Length - b.ByteLength) > 3) throw new ArgumentException($"{nameof(buffer)} byte size is larger than declared");
            }

            if (brefcount == 0 && buffer != null)
                throw new ArgumentNullException($"{nameof(buffer)} must be null");


            var jsonChunk = JsonSerializer.SerializeToUtf8Bytes(model);
            var jsonPadding = jsonChunk.Length & 3; if (jsonPadding != 0) jsonPadding = 4 - jsonPadding;

            if (buffer != null && buffer.Length == 0) buffer = null;
            var binPadding = buffer == null ? 0 : buffer.Length & 3; if (binPadding != 0) binPadding = 4 - binPadding;

            int fullLength = 4 + 4 + 4;

            fullLength += 8 + jsonChunk.Length + jsonPadding;
            if (buffer != null) fullLength += 8 + buffer.Length + binPadding;

            binaryWriter.Write(GLTFHEADER);
            binaryWriter.Write(GLTFVERSION2);
            binaryWriter.Write(fullLength);

            binaryWriter.Write(jsonChunk.Length + jsonPadding);
            binaryWriter.Write(CHUNKJSON);
            binaryWriter.Write(jsonChunk);
            for (int i = 0; i < jsonPadding; ++i) binaryWriter.Write((byte)0x20);

            if (buffer != null)
            {
                binaryWriter.Write(buffer.Length + binPadding);
                binaryWriter.Write(CHUNKBIN);
                binaryWriter.Write(buffer);
                for (int i = 0; i < binPadding; ++i) binaryWriter.Write((byte)0);
            }
        }


        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable binary writer and pack all data into the model
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="outputFile">GLB output file path</param>
        /// <param name="gltfFilePath">Source file path used to load the model</param>
        /// <param name="glbBinChunck">optional GLB-stored Buffer (BIN data file). If null, then the first buffer Uri must point to a BIN file.</param>
        public static void SaveBinaryModelPacked(this Gltf model, string outputFile, string gltfFilePath, byte[] glbBinChunck = null)
        {
            using Stream stream = File.Create(outputFile);
            using var wb = new BinaryWriter(stream);
            SaveBinaryModelPacked(model, wb, gltfFilePath, glbBinChunck);
        }

        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable binary writer and pack all data into the model
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="binaryWriter">Binary Writer</param>
        /// <param name="gltfFilePath">Source file path used to load the model</param>
        /// <param name="glbBinChunck">optional GLB-stored Buffer (BIN data file). If null, then the first buffer Uri must point to a BIN file.</param>
        public static void SaveBinaryModelPacked(this Gltf model, BinaryWriter binaryWriter, string gltfFilePath, byte[] glbBinChunck = null)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            byte[] binBufferData = null;

            using (var memoryStream = new MemoryStream())
            {
                var bufferViews = new List<BufferView>();
                var bufferData = new Dictionary<int, byte[]>();

                if (model.BufferViews != null)
                {
                    foreach (var bufferView in model.BufferViews)
                    {
                        memoryStream.Align(4);

                        var byteOffset = memoryStream.Position;

                        if (!bufferData.TryGetValue(bufferView.Buffer, out byte[] data))
                        {
                            // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#glb-stored-buffer
                            // "glTF Buffer referring to GLB-stored BIN chunk, must have buffer.uri 
                            // property undefined, and it must be the first element of buffers array"
                            data = bufferView.Buffer == 0 && glbBinChunck != null
                                        ? glbBinChunck
                                        : model.LoadBinaryBuffer(bufferView.Buffer, gltfFilePath);

                            bufferData.Add(bufferView.Buffer, data);
                        }

                        memoryStream.Write(data, bufferView.ByteOffset, bufferView.ByteLength);

                        bufferView.Buffer = 0;
                        bufferView.ByteOffset = (int)byteOffset;
                        bufferViews.Add(bufferView);
                    }
                }

                if (model.Images != null)
                {
                    for (var index = 0; index < model.Images.Length; index++)
                    {
                        var byteOffset = memoryStream.Position;

                        var data = model.OpenImageFile(index, gltfFilePath);
                        data.CopyTo(memoryStream);

                        var image = model.Images[index];
                        image.BufferView = bufferViews.Count;
                        image.MimeType = GetMimeType(image.Uri);
                        image.Uri = null;

                        bufferViews.Add(new BufferView
                        {
                            Buffer = 0,
                            ByteOffset = (int)byteOffset,
                            ByteLength = (int)data.Length,
                        });
                    }
                }

                if (bufferViews.Any())
                {
                    model.BufferViews = bufferViews.ToArray();

                    model.Buffers = new[]
                    {
                        new Schema.Buffer
                        {
                            ByteLength = (int)memoryStream.Length
                        }
                    };

                    binBufferData = memoryStream.ToArray();
                }
            }

            SaveBinaryModel(model, binBufferData, binaryWriter);
        }

        /// <summary>
        /// Converts self contained GLB to glTF file and associated textures and data
        /// </summary>
        /// <param name="inputFilePath">glTF binary file (.glb) to unpack</param>
        /// <param name="outputDirectoryPath">Directory where the files will be extracted</param>
        public static void Unpack(string inputFilePath, string outputDirectoryPath)
        {
            if (!File.Exists(inputFilePath))
                throw new ArgumentException("Input file does not exists");
            if (!Directory.Exists(outputDirectoryPath))
                throw new ArgumentException("Ouput directory does not exists");

            string inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string inputDirectoryPath = Path.GetDirectoryName(inputFilePath);

            var model = LoadModelAsync(inputFilePath).Result;
            GltfLoader.Schema.Buffer binBuffer = null;
            byte[] binBufferData = null;
            if (model.Buffers != null && string.IsNullOrEmpty(model.Buffers[0].Uri))
            {
                binBuffer = model.Buffers[0];
                binBufferData = model.LoadBinaryBuffer(0, inputFilePath);
            }

            var imageBufferViewIndices = new List<int>();

            if (model.Images != null)
            {
                for (var index = 0; index < model.Images.Length; index++)
                {
                    var image = model.Images[index];

                    if (!string.IsNullOrEmpty(image.Uri))
                    {
                        if (!image.Uri.StartsWith("data:"))
                        {
                            var sourceFilePath = Path.Combine(inputDirectoryPath, image.Uri);
                            var fileName = $"{inputFilePath}_image{index}.bin";

                            if (File.Exists(sourceFilePath))
                            {
                                var destinationFilePath = Path.Combine(outputDirectoryPath, fileName);
                                File.Copy(sourceFilePath, destinationFilePath, true);
                            }

                            image.Uri = fileName;
                        }
                    }
                    else if (image.BufferView.HasValue)
                    {
                        var bufferView = model.BufferViews[image.BufferView.Value];
                        if (bufferView.Buffer == 0)
                        {
                            imageBufferViewIndices.Add(image.BufferView.Value);

                            var fileExtension = image.MimeType == "image/jpeg" ? "jpg" : "png";
                            var fileName = $"{inputFileName}_image{index}.{fileExtension}";

                            using (var fileStream = File.Create(Path.Combine(outputDirectoryPath, fileName)))
                            {
                                fileStream.Write(binBufferData, bufferView.ByteOffset, bufferView.ByteLength);
                            }

                            image.BufferView = null;
                            image.MimeType = null;
                            image.Uri = fileName;
                        }
                    }
                }
            }

            if (model.BufferViews != null)
            {
                var binFileName = $"{inputFileName}.bin";
                var binFilePath = Path.Combine(outputDirectoryPath, binFileName);
                var binByteLength = 0;

                var indexMap = new Dictionary<int, int>();
                var bufferViews = new List<BufferView>();
                using (var fileStream = File.Create(binFilePath))
                {
                    for (var index = 0; index < model.BufferViews.Length; index++)
                    {
                        if (!imageBufferViewIndices.Any(imageIndex => imageIndex == index))
                        {
                            var bufferView = model.BufferViews[index];
                            if (bufferView.Buffer == 0)
                            {
                                fileStream.Align(4);
                                var fileStreamPosition = fileStream.Position;
                                fileStream.Write(binBufferData, bufferView.ByteOffset, bufferView.ByteLength);
                                bufferView.ByteOffset = (int)fileStreamPosition;
                            }

                            var newIndex = bufferViews.Count;
                            if (index != newIndex)
                            {
                                indexMap.Add(index, newIndex);
                            }

                            bufferViews.Add(bufferView);
                        }
                    }

                    binByteLength = (int)fileStream.Length;
                }

                model.BufferViews = bufferViews.ToArray();

                if (binByteLength == 0)
                {
                    File.Delete(binFilePath);
                    if (binBuffer != null)
                    {
                        model.Buffers = model.Buffers.Skip(1).ToArray();
                        foreach (var bufferView in model.BufferViews)
                        {
                            bufferView.Buffer--;
                        }
                    }
                }
                else
                {
                    binBuffer.Uri = binFileName;
                    binBuffer.ByteLength = binByteLength;
                }

                if (model.Accessors != null)
                {
                    foreach (var accessor in model.Accessors)
                    {
                        if (accessor.BufferView.HasValue)
                        {
                            if (indexMap.TryGetValue(accessor.BufferView.Value, out int newIndex))
                            {
                                accessor.BufferView = newIndex;
                            }
                        }
                    }
                }
            }

            if (model.Buffers != null)
            {
                for (var index = 1; index < model.Buffers.Length; index++)
                {
                    var buffer = model.Buffers[index];
                    if (!buffer.Uri.StartsWith("data:"))
                    {
                        var sourceFilePath = Path.Combine(inputDirectoryPath, buffer.Uri);
                        var fileName = $"{inputFileName}{index}.bin";

                        if (File.Exists(sourceFilePath))
                        {
                            var destinationFilePath = Path.Combine(outputDirectoryPath, fileName);
                            File.Copy(sourceFilePath, destinationFilePath, true);
                        }

                        buffer.Uri = fileName;
                    }
                }
            }

            SaveModel(model, Path.Combine(outputDirectoryPath, $"{inputFileName}.gltf"));
        }

        /// <summary>
        /// Converts a glTF file and its associated resources to a packed GLB binary file
        /// </summary>
        /// <param name="inputGltfFilePath">glTF file (.gltf) to pack</param>
        /// <param name="outputGlbFile">Path where the GLB file should be generated</param>
        public static void Pack(string inputGltfFilePath, string outputGlbFile)
        {
            if (!File.Exists(inputGltfFilePath))
                throw new ArgumentException("glTF file does not exists.", nameof(inputGltfFilePath));

            Gltf model = LoadModelAsync(inputGltfFilePath).Result;

            SaveBinaryModelPacked(model, outputGlbFile, inputGltfFilePath);
        }

        private static string GetMimeType(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return null;
            }

            if (uri.StartsWith("data:image/png;base64,") || uri.EndsWith(".png"))
            {
                return "image/png";
            }

            if (uri.StartsWith("data:image/jpeg;base64,") || uri.EndsWith(".jpg") || uri.EndsWith(".jpeg"))
            {
                return "image/jpeg";
            }

            throw new InvalidOperationException("Unable to determine mime type from URI.");
        }
    }
}
