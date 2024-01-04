CC       = gcc 
CFLAGS   = -Wall -g -O -fPIC 
CXXFLAGS = 
TARGET   = libModLuaNative.so

OBJS     = ModLuaNative.o LuaImport.o
SRCS     = ModLuaNative.cpp LuaImport.cpp

default all: $(OBJS)
	$(CC) -shared -fPIC -o $(TARGET) $(OBJS)
	strip -x $(TARGET)

$(OBJS): $(SRCS)
	$(CC) $(CFLAGS) -c $^

clean:
	rm -f *.o
	rm -f *.so
