import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Libro } from '../../../core/models/libro.model';
import { Categoria } from '../../../core/models/categoria.model';
import { LibroService } from '../../../core/services/libro.service';
import { CategoriaService } from '../../../core/services/categoria.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';

@Component({
  selector: 'app-lista-libros',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './lista-libros.html',
  styleUrl: './lista-libros.scss'
})
export class ListaLibrosComponent implements OnInit {
  libros: Libro[] = [];
  categorias: Categoria[] = [];
  cargando = true;
  error = '';

  textoBusqueda = '';
  categoriaSeleccionada: number | null = null;

  // Nuevo: controla si mostramos el hero de "categoría destacada" o no
  mostrarHeroCategorias = true;
  tituloPagina = 'Biblioteca de Prácticas';

  constructor(
    private libroService: LibroService,
    private categoriaService: CategoriaService,
    private route: ActivatedRoute,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.cargarLibros();
    this.categoriaService.getCategorias().subscribe({
      next: (data) => (this.categorias = data),
      error: (err) => console.error('Error cargando categorías', err)
    });

    // Si la ruta trae ?categoria=5, filtra directo por esa categoría
    this.route.queryParams.subscribe((params) => {
      const catId = params['categoria'] ? Number(params['categoria']) : null;
      this.categoriaSeleccionada = catId;
      this.mostrarHeroCategorias = !catId; // oculta el hero grande si vienes filtrado
    });

    // Si la ruta es /todos-los-libros, no mostramos el hero de categorías
    if (this.route.snapshot.data['ocultarHero']) {
      this.mostrarHeroCategorias = false;
      this.tituloPagina = 'Todos los libros';
    }
  }

  cargarLibros(): void {
    this.cargando = true;
    this.libroService.getLibros().subscribe({
      next: (data) => {
        this.libros = data;
        this.cargando = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron cargar los libros.';
        this.cargando = false;
      }
    });
  }

  get librosFiltrados(): Libro[] {
    return this.libros.filter((libro) => {
      const coincideTexto =
        !this.textoBusqueda ||
        libro.titulo.toLowerCase().includes(this.textoBusqueda.toLowerCase()) ||
        (libro.autor?.toLowerCase().includes(this.textoBusqueda.toLowerCase()) ?? false);

      const coincideCategoria =
        this.categoriaSeleccionada === null ||
        libro.categoriaId === this.categoriaSeleccionada;

      return coincideTexto && coincideCategoria;
    });
  }

  seleccionarCategoria(id: number | null): void {
    this.categoriaSeleccionada = id;
  }

  colorPortada(id: number): string {
    const colores = ['bg-marino-700', 'bg-ambar-600', 'bg-marino-900', 'bg-marino-400'];
    return colores[id % colores.length];
  }

  iniciales(titulo: string): string {
    return titulo
      .split(' ')
      .filter(w => w.length > 0)
      .slice(0, 2)
      .map(w => w[0].toUpperCase())
      .join('');
  }

  urlArchivo(ruta: string): string {
    return `http://localhost:5250${ruta}`;
  }
}