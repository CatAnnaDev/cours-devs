CXX ?= c++
CXXFLAGS ?= -std=c++17 -O2 -Wall -Wextra

cpplings: cpplings.cpp catalogue.cpp catalogue.hpp
	$(CXX) $(CXXFLAGS) -o cpplings cpplings.cpp catalogue.cpp

run: cpplings
	./cpplings

verify: cpplings
	./cpplings verify

compile_commands.json:
	@printf '[\n' > $@
	@first=1; for f in exercices/*/*.cpp solutions/*/*.cpp; do \
		if [ $$first -eq 0 ]; then printf ',\n' >> $@; fi; first=0; \
		printf '  {"directory": "%s", "file": "%s", "command": "c++ -std=c++23 -Wall -Wextra -I. -c %s"}' \
			"$$(pwd)" "$$f" "$$f" >> $@; \
	done
	@printf '\n]\n' >> $@
	@echo "compile_commands.json genere"

clean:
	rm -f cpplings
	rm -rf .travail

.PHONY: run verify clean
