CC       = gcc 
CFLAGS   = -Wall -g -O -fPIC 
CXXFLAGS = 
TARGET   = libLuaJsonNative.so

OBJS     = LuaJsonNative.o LuaImport.o
SRCS     = LuaJsonNative.cpp LuaImport.cpp

default all: $(OBJS)
	$(CC) -shared -fPIC -o $(TARGET) $(OBJS)
	strip -x $(TARGET)

$(OBJS): $(SRCS)
	$(CC) $(CFLAGS) -c $^

clean:
	rm -f *.o
	rm -f *.so
