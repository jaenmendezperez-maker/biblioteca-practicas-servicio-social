import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Categoria } from '../../../core/models/categoria.model';
import { Libro } from '../../../core/models/libro.model';
import { CategoriaService } from '../../../core/services/categoria.service';
import { LibroService } from '../../../core/services/libro.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';

@Component({
  selector: 'app-lista-categorias',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './lista-categorias.html'
})
export class ListaCategoriasComponent implements OnInit {
  categorias: Categoria[] = [];
  libros: Libro[] = [];
  cargando = true;
  textoBusqueda = '';

  constructor(
    private categoriaService: CategoriaService,
    private libroService: LibroService,
    private router: Router,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.categoriaService.getCategorias().subscribe({
      next: (data) => {
        this.categorias = data;
        this.libroService.getLibros().subscribe({
          next: (libros) => {
            this.libros = libros;
            this.cargando = false;
          },
          error: () => (this.cargando = false)
        });
      },
      error: () => (this.cargando = false)
    });
  }

  get categoriasFiltradas(): Categoria[] {
    if (!this.textoBusqueda) return this.categorias;
    return this.categorias.filter((c) =>
      c.nombre.toLowerCase().includes(this.textoBusqueda.toLowerCase())
    );
  }

  contarLibros(categoriaId: number): number {
    return this.libros.filter((l) => l.categoriaId === categoriaId).length;
  }

  irACategoria(id: number): void {
    this.router.navigate(['/'], { queryParams: { categoria: id } });
  }
}