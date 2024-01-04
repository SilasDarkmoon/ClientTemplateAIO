make clean
make BUILDMODE=dynamic TARGET=x86_64 TARGET_FLAGS=" -arch x86_64 "
install_name_tool -change /usr/local/lib/libluajit-5.1.2.dylib @executable_path/libluajit.so ./src/luajit
mv ./src/libluajit.so ./libluajit_x86_64.so
mv ./src/luajit ./luajit_x86_64

make clean
make BUILDMODE=dynamic TARGET=arm64 TARGET_FLAGS=" -arch arm64 "
install_name_tool -change /usr/local/lib/libluajit-5.1.2.dylib @executable_path/libluajit.so ./src/luajit
mv ./src/libluajit.so ./libluajit_arm64.so
mv ./src/luajit ./luajit_arm64

lipo -create -output ./libluajit.so ./libluajit_x86_64.so ./libluajit_arm64.so
lipo -create -output ./luajit ./luajit_x86_64 ./luajit_arm64

make clean
